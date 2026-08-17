using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.LineStageSequences;
using ProductionMES.Application.Services.LineStageSequences;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho LineStageSequenceService, bám theo AC1-AC7 của US-03 (Documents/BACKLOG-user-story.md) — trình
/// tự công đoạn (Stage nào, thứ tự nào) là cấu hình CỦA LINE, thiết lập 1 lần dùng chung cho mọi kế hoạch chạy
/// trên Line đó (sửa lại 17/08/2026, thay thế hoàn toàn phần AddAsync/RemoveAsync/ReorderAsync trước đây từng
/// nằm ở ProductionPlanStageService).
/// </summary>
public class LineStageSequenceServiceTests
{
    private readonly Mock<IRepository<LineStageSequence>> _repositoryMock = new();
    private readonly Mock<IRepository<Line>> _lineRepositoryMock = new();
    private readonly Mock<IRepository<Stage>> _stageRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlanStage>> _productionPlanStageRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly LineStageSequenceService _sut;

    public LineStageSequenceServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<LineStageSequence>()).Returns(_repositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Line>()).Returns(_lineRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _sut = new LineStageSequenceService(_unitOfWorkMock.Object);

        _lineRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Line { Id = 1, Name = "Line 1", IsActive = true });
        _stageRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object id, CancellationToken _) => new Stage { Id = (int)id, Name = $"Stage {id}", IsActive = true });

        // Mặc định Line chưa có kế hoạch nào Running/Paused tại bất kỳ công đoạn nào — override khi test AC2 (chặn gỡ).
        SetupActivePlanStages(new List<ProductionPlanStage>());
    }

    private void SetupSequence(List<LineStageSequence> items)
    {
        _repositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LineStageSequence, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<LineStageSequence, bool>> predicate, CancellationToken _) =>
                items.Where(predicate.Compile()).ToList());
    }

    private void SetupActivePlanStages(List<ProductionPlanStage> items)
    {
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                items.Where(predicate.Compile()).ToList());
    }

    // AC1 — Thêm công đoạn vào trình tự của Line: chưa có SequenceNumber -> thêm vào cuối danh sách (mặc định).
    [Fact]
    public async Task AddAsync_ChuaCoCongDoanNao_ThemVaoViTri1()
    {
        SetupSequence(new List<LineStageSequence>());

        var result = await _sut.AddAsync(1, new AddStageToLineRequest { StageId = 10 });

        Assert.Equal(1, result.SequenceNumber);
        Assert.Null(result.PreviousStageId);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<LineStageSequence>(x => x.LineId == 1 && x.StageId == 10), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_DaCo2CongDoan_ThemVaoCuoiDanhSachTheoMacDinh()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, LineId = 1, StageId = 20, SequenceNumber = 2 },
        };
        SetupSequence(existing);

        var result = await _sut.AddAsync(1, new AddStageToLineRequest { StageId = 30 });

        Assert.Equal(3, result.SequenceNumber);
        Assert.Equal(20, result.PreviousStageId); // liền trước = công đoạn có SequenceNumber = 3 - 1 = 2
    }

    // AC5 — Từ chối khi tạo vòng lặp: mô hình đảm bảo qua ràng buộc "1 công đoạn không lặp lại trong 1 Line".
    [Fact]
    public async Task AddAsync_CongDoanDaCoTrongTrinhTu_NemBusinessRuleException()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
        };
        SetupSequence(existing);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.AddAsync(1, new AddStageToLineRequest { StageId = 10 }));
    }

    // AC4 — Từ chối khi trùng số thứ tự.
    [Fact]
    public async Task AddAsync_TrungSoThuTuDaChiDinh_NemBusinessRuleException()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
        };
        SetupSequence(existing);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.AddAsync(1, new AddStageToLineRequest { StageId = 20, SequenceNumber = 1 }));
    }

    [Fact]
    public async Task AddAsync_LineKhongTonTai_NemEntityNotFoundException()
    {
        SetupSequence(new List<LineStageSequence>());

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _sut.AddAsync(999, new AddStageToLineRequest { StageId = 10 }));
    }

    // AC2 — Gỡ công đoạn khỏi trình tự của Line: trình tự còn lại được đánh số lại liên tục.
    [Fact]
    public async Task RemoveAsync_GoCongDoanODau_ConLaiDuocDanhSoLaiLienTuc()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, LineId = 1, StageId = 20, SequenceNumber = 2 },
            new() { Id = 3, LineId = 1, StageId = 30, SequenceNumber = 3 },
        };
        SetupSequence(existing);

        await _sut.RemoveAsync(1, 10);

        _repositoryMock.Verify(r => r.Remove(existing[0]), Times.Once);
        Assert.Equal(1, existing[1].SequenceNumber); // công đoạn 20 từ vị trí 2 -> 1
        Assert.Equal(2, existing[2].SequenceNumber); // công đoạn 30 từ vị trí 3 -> 2
    }

    [Fact]
    public async Task RemoveAsync_CongDoanKhongThuocTrinhTu_NemEntityNotFoundException()
    {
        SetupSequence(new List<LineStageSequence>());

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.RemoveAsync(1, 999));
    }

    // AC2 — Chặn hẳn khi đang có kế hoạch Running tại đúng công đoạn sắp gỡ.
    [Fact]
    public async Task RemoveAsync_DangCoKeHoachRunningTaiCongDoan_NemBusinessRuleException()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
        };
        SetupSequence(existing);
        SetupActivePlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 5, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running },
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.RemoveAsync(1, 10));
        _repositoryMock.Verify(r => r.Remove(It.IsAny<LineStageSequence>()), Times.Never);
    }

    // AC2 — Chặn hẳn khi đang có kế hoạch Paused tại đúng công đoạn sắp gỡ (không chỉ Running).
    [Fact]
    public async Task RemoveAsync_DangCoKeHoachPausedTaiCongDoan_NemBusinessRuleException()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
        };
        SetupSequence(existing);
        SetupActivePlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 5, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Paused },
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.RemoveAsync(1, 10));
    }

    // AC2 — Kế hoạch đã Completed/Cancelled tại công đoạn đó KHÔNG chặn việc gỡ (chỉ Running/Paused mới chặn).
    [Fact]
    public async Task RemoveAsync_ChiCoKeHoachDaCompleted_KhongBiChan()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
        };
        SetupSequence(existing);
        SetupActivePlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 5, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Completed },
        });

        await _sut.RemoveAsync(1, 10);

        _repositoryMock.Verify(r => r.Remove(existing[0]), Times.Once);
    }

    // AC3 — Sắp xếp lại trình tự: lưu đúng trình tự mới, tự xác định lại công đoạn liền trước.
    [Fact]
    public async Task ReorderAsync_HoanDoi2ViTri_LuuDungTrinhTuMoiVaSuyRaLienTruocDung()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, LineId = 1, StageId = 20, SequenceNumber = 2 },
        };
        SetupSequence(existing);

        var request = new ReorderLineStageSequenceRequest
        {
            Items = new List<ReorderLineStageSequenceItem>
            {
                new() { StageId = 20, SequenceNumber = 1 },
                new() { StageId = 10, SequenceNumber = 2 },
            },
        };

        var result = await _sut.ReorderAsync(1, request);

        var stage20 = result.Single(x => x.StageId == 20);
        var stage10 = result.Single(x => x.StageId == 10);
        Assert.Equal(1, stage20.SequenceNumber);
        Assert.Null(stage20.PreviousStageId);
        Assert.Equal(2, stage10.SequenceNumber);
        Assert.Equal(20, stage10.PreviousStageId);
    }

    // AC4 — Từ chối khi trùng số thứ tự trong danh sách sắp xếp.
    [Fact]
    public async Task ReorderAsync_TrungSoThuTu_NemBusinessRuleException()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, LineId = 1, StageId = 20, SequenceNumber = 2 },
        };
        SetupSequence(existing);

        var request = new ReorderLineStageSequenceRequest
        {
            Items = new List<ReorderLineStageSequenceItem>
            {
                new() { StageId = 10, SequenceNumber = 1 },
                new() { StageId = 20, SequenceNumber = 1 },
            },
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ReorderAsync(1, request));
    }

    // AC5 — Từ chối khi cấu hình dẫn đến vòng lặp: ở đây thể hiện qua việc 1 công đoạn bị lặp lại trong danh
    // sách gửi lên (vi phạm điều kiện cấu trúc "1 công đoạn - tối đa 1 vị trí").
    [Fact]
    public async Task ReorderAsync_CongDoanLapLaiTrongDanhSach_NemBusinessRuleException()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, LineId = 1, StageId = 20, SequenceNumber = 2 },
        };
        SetupSequence(existing);

        var request = new ReorderLineStageSequenceRequest
        {
            Items = new List<ReorderLineStageSequenceItem>
            {
                new() { StageId = 10, SequenceNumber = 1 },
                new() { StageId = 10, SequenceNumber = 2 },
            },
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ReorderAsync(1, request));
    }

    [Fact]
    public async Task GetByLineAsync_TraVeDungTrinhTuVaLienTruoc()
    {
        var existing = new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, LineId = 1, StageId = 20, SequenceNumber = 2 },
            new() { Id = 3, LineId = 1, StageId = 30, SequenceNumber = 3 },
        };
        SetupSequence(existing);

        var result = await _sut.GetByLineAsync(1);

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { 10, 20, 30 }, result.Select(x => x.StageId));
        Assert.Null(result[0].PreviousStageId);
        Assert.Equal(10, result[1].PreviousStageId);
        Assert.Equal(20, result[2].PreviousStageId);
    }
}
