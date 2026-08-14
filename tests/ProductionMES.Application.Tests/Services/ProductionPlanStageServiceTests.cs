using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho ProductionPlanStageService, bám theo AC1-AC5 của US-03 và AC1-AC7 của US-05a
/// (Documents/BACKLOG-user-story.md).
/// </summary>
public class ProductionPlanStageServiceTests
{
    private readonly Mock<IRepository<ProductionPlanStage>> _repositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<Stage>> _stageRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ProductionPlanStageService _sut;

    public ProductionPlanStageServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_repositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _sut = new ProductionPlanStageService(_unitOfWorkMock.Object);

        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionPlan { Id = 1, LineId = 1, PlannedQuantity = 1000 });
        _stageRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object id, CancellationToken _) => new Stage { Id = (int)id, Name = $"Stage {id}", IsActive = true });

        // Mặc định không có lượt scan OK nào — từng test override qua SetupOkScans khi cần kiểm tra RunCount/RemainingCount.
        SetupOkScans(new List<Scan>());
    }

    private void SetupOkScans(List<Scan> okScans)
    {
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Scan, bool>> predicate, CancellationToken _) =>
                okScans.Where(predicate.Compile()).ToList());
    }

    private void SetupPlanStages(List<ProductionPlanStage> items)
    {
        _repositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                items.Where(predicate.Compile()).ToList());
    }

    // AC1 — Thêm công đoạn vào kế hoạch: chưa có SequenceNumber -> thêm vào cuối danh sách (mặc định).
    [Fact]
    public async Task AddAsync_ChuaCoCongDoanNao_ThemVaoViTri1()
    {
        SetupPlanStages(new List<ProductionPlanStage>());

        var result = await _sut.AddAsync(1, new AddStageToProductionPlanRequest { StageId = 10 });

        Assert.Equal(1, result.SequenceNumber);
        Assert.Null(result.PreviousStageId);
        Assert.Equal(PlanStatus.Draft, result.PlanStatus);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<ProductionPlanStage>(x => x.LineId == 1 && x.PlanStatus == PlanStatus.Draft), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_DaCo2CongDoan_ThemVaoCuoiDanhSachTheoMacDinh()
    {
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1 },
            new() { Id = 2, ProductionPlanId = 1, StageId = 20, LineId = 1, SequenceNumber = 2 },
        };
        SetupPlanStages(existing);

        var result = await _sut.AddAsync(1, new AddStageToProductionPlanRequest { StageId = 30 });

        Assert.Equal(3, result.SequenceNumber);
        Assert.Equal(20, result.PreviousStageId); // liền trước = công đoạn có SequenceNumber = 3 - 1 = 2
    }

    // AC5 — Từ chối khi tạo vòng lặp: mô hình đảm bảo qua ràng buộc "1 công đoạn không lặp lại trong 1 kế hoạch".
    [Fact]
    public async Task AddAsync_CongDoanDaCoTrongKeHoach_NemBusinessRuleException()
    {
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1 },
        };
        SetupPlanStages(existing);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.AddAsync(1, new AddStageToProductionPlanRequest { StageId = 10 }));
    }

    // AC4 — Từ chối khi trùng số thứ tự.
    [Fact]
    public async Task AddAsync_TrungSoThuTuDaChiDinh_NemBusinessRuleException()
    {
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1 },
        };
        SetupPlanStages(existing);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.AddAsync(1, new AddStageToProductionPlanRequest { StageId = 20, SequenceNumber = 1 }));
    }

    // AC2 — Gỡ công đoạn khỏi kế hoạch: trình tự còn lại được đánh số lại liên tục.
    [Fact]
    public async Task RemoveAsync_GoCongDoanODau_ConLaiDuocDanhSoLaiLienTuc()
    {
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1 },
            new() { Id = 2, ProductionPlanId = 1, StageId = 20, LineId = 1, SequenceNumber = 2 },
            new() { Id = 3, ProductionPlanId = 1, StageId = 30, LineId = 1, SequenceNumber = 3 },
        };
        SetupPlanStages(existing);

        await _sut.RemoveAsync(1, 10);

        _repositoryMock.Verify(r => r.Remove(existing[0]), Times.Once);
        Assert.Equal(1, existing[1].SequenceNumber); // công đoạn 20 từ vị trí 2 -> 1
        Assert.Equal(2, existing[2].SequenceNumber); // công đoạn 30 từ vị trí 3 -> 2
    }

    [Fact]
    public async Task RemoveAsync_CongDoanKhongThuocKeHoach_NemEntityNotFoundException()
    {
        SetupPlanStages(new List<ProductionPlanStage>());

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.RemoveAsync(1, 999));
    }

    // AC3 — Sắp xếp lại trình tự: lưu đúng trình tự mới, tự xác định lại công đoạn liền trước.
    [Fact]
    public async Task ReorderAsync_HoanDoi2ViTri_LuuDungTrinhTuMoiVaSuyRaLienTruocDung()
    {
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1 },
            new() { Id = 2, ProductionPlanId = 1, StageId = 20, LineId = 1, SequenceNumber = 2 },
        };
        SetupPlanStages(existing);

        var request = new ReorderProductionPlanStageRequest
        {
            Items = new List<ReorderProductionPlanStageItem>
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
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1 },
            new() { Id = 2, ProductionPlanId = 1, StageId = 20, LineId = 1, SequenceNumber = 2 },
        };
        SetupPlanStages(existing);

        var request = new ReorderProductionPlanStageRequest
        {
            Items = new List<ReorderProductionPlanStageItem>
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
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1 },
            new() { Id = 2, ProductionPlanId = 1, StageId = 20, LineId = 1, SequenceNumber = 2 },
        };
        SetupPlanStages(existing);

        var request = new ReorderProductionPlanStageRequest
        {
            Items = new List<ReorderProductionPlanStageItem>
            {
                new() { StageId = 10, SequenceNumber = 1 },
                new() { StageId = 10, SequenceNumber = 2 },
            },
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ReorderAsync(1, request));
    }

    // US-05a AC4 — "Đã chạy"/"còn lại" tính động từ lịch sử scan OK, không đọc từ cột số liệu tĩnh.
    [Fact]
    public async Task GetByProductionPlanAsync_DaCo400ScanOk_TraVeDaChay400ConLai600()
    {
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Paused },
        };
        SetupPlanStages(existing);
        SetupOkScans(Enumerable.Range(1, 400)
            .Select(i => new Scan { ProductionPlanId = 1, StageId = 10, Result = ScanResult.Ok, TagCode = $"TAG{i}" })
            .ToList());

        var result = await _sut.GetByProductionPlanAsync(1);

        var stage = result.Single();
        Assert.Equal(400, stage.RunCount);
        Assert.Equal(600, stage.RemainingCount);
    }

    // US-05a AC1 — Áp dụng kế hoạch (Draft) cho công đoạn, chưa có kế hoạch khác Running cùng (Line, Công đoạn) -> Running.
    [Fact]
    public async Task ApplyAsync_ChuaCoKeHoachKhacRunning_ChuyenRunningThanhCong()
    {
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Draft };
        SetupPlanStages(new List<ProductionPlanStage> { item });

        var result = await _sut.ApplyAsync(1, 10);

        Assert.Equal(PlanStatus.Running, result.PlanStatus);
        Assert.Equal(PlanStatus.Running, item.PlanStatus);
        _repositoryMock.Verify(r => r.Update(item), Times.Once);
    }

    // US-05a AC1 — (Line, Công đoạn) đang có kế hoạch KHÁC Running -> từ chối, yêu cầu Tạm dừng/Đóng trước.
    [Fact]
    public async Task ApplyAsync_LineCongDoanDangCoKeHoachKhacRunning_NemBusinessRuleException()
    {
        var item = new ProductionPlanStage { Id = 2, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Draft };
        var runningOther = new ProductionPlanStage { Id = 1, ProductionPlanId = 99, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Running };

        // Repository FindAsync trả về dữ liệu tùy theo productionPlanId trong predicate — mô phỏng 2 truy vấn khác nhau
        // trong ApplyAsync: 1) lấy item theo (ProductionPlanId=1), 2) tìm Running theo (LineId, StageId) toàn hệ thống.
        _repositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                new List<ProductionPlanStage> { item, runningOther }.Where(predicate.Compile()).ToList());

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ApplyAsync(1, 10));
        Assert.Equal(PlanStatus.Draft, item.PlanStatus); // không đổi trạng thái khi bị từ chối
    }

    // US-05a AC2 — Công đoạn KHÁC của cùng Line được áp dụng kế hoạch khác song song, không bị chặn.
    [Fact]
    public async Task ApplyAsync_CongDoanKhacCungLineDangRunning_KhongBiChan()
    {
        var item = new ProductionPlanStage { Id = 2, ProductionPlanId = 1, StageId = 20, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Draft };
        var runningStageA = new ProductionPlanStage { Id = 1, ProductionPlanId = 99, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Running };

        _repositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                new List<ProductionPlanStage> { item, runningStageA }.Where(predicate.Compile()).ToList());

        var result = await _sut.ApplyAsync(1, 20);

        Assert.Equal(PlanStatus.Running, result.PlanStatus);
    }

    // US-05a AC7 — Completed/Cancelled không tự "Áp dụng" lại được.
    [Theory]
    [InlineData(PlanStatus.Completed)]
    [InlineData(PlanStatus.Cancelled)]
    public async Task ApplyAsync_DaCompletedHoacCancelled_NemBusinessRuleException(PlanStatus status)
    {
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = status };
        SetupPlanStages(new List<ProductionPlanStage> { item });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ApplyAsync(1, 10));
    }

    // US-05a AC3 — Tạm dừng: Running -> Paused, giữ nguyên (không xóa) tiến độ vì tính động.
    [Fact]
    public async Task PauseAsync_DangRunning_ChuyenPausedThanhCong()
    {
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Running };
        SetupPlanStages(new List<ProductionPlanStage> { item });

        var result = await _sut.PauseAsync(1, 10);

        Assert.Equal(PlanStatus.Paused, result.PlanStatus);
        _repositoryMock.Verify(r => r.Update(item), Times.Once);
    }

    [Fact]
    public async Task PauseAsync_KhongPhaiRunning_NemBusinessRuleException()
    {
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Draft };
        SetupPlanStages(new List<ProductionPlanStage> { item });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.PauseAsync(1, 10));
    }

    // US-05a AC6 — Đóng sớm khi chưa đủ số lượng mà chưa Confirm -> từ chối, nêu rõ số lượng còn thiếu.
    [Fact]
    public async Task CloseAsync_ChuaDuSoLuongChuaConfirm_NemBusinessRuleException()
    {
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Running };
        SetupPlanStages(new List<ProductionPlanStage> { item });
        SetupOkScans(Enumerable.Range(1, 400)
            .Select(i => new Scan { ProductionPlanId = 1, StageId = 10, Result = ScanResult.Ok, TagCode = $"TAG{i}" })
            .ToList()); // 400/1000

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CloseAsync(1, 10, new CloseProductionPlanStageRequest { Confirm = false }));
        Assert.Equal(PlanStatus.Running, item.PlanStatus); // chưa đổi trạng thái
    }

    // US-05a AC6 — Đóng sớm khi chưa đủ số lượng, đã Confirm -> chuyển Cancelled.
    [Fact]
    public async Task CloseAsync_ChuaDuSoLuongDaConfirm_ChuyenCancelledThanhCong()
    {
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Running };
        SetupPlanStages(new List<ProductionPlanStage> { item });
        SetupOkScans(Enumerable.Range(1, 400)
            .Select(i => new Scan { ProductionPlanId = 1, StageId = 10, Result = ScanResult.Ok, TagCode = $"TAG{i}" })
            .ToList());

        var result = await _sut.CloseAsync(1, 10, new CloseProductionPlanStageRequest { Confirm = true });

        Assert.Equal(PlanStatus.Cancelled, result.PlanStatus);
        Assert.Equal(PlanStatus.Cancelled, item.PlanStatus);
    }

    [Fact]
    public async Task CloseAsync_DaCompleted_NemBusinessRuleException()
    {
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, SequenceNumber = 1, PlanStatus = PlanStatus.Completed };
        SetupPlanStages(new List<ProductionPlanStage> { item });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CloseAsync(1, 10, new CloseProductionPlanStageRequest { Confirm = true }));
    }
}
