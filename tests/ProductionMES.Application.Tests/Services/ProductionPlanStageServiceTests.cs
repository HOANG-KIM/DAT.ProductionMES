using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>Unit test cho ProductionPlanStageService, bám theo AC1-AC5 của US-03 (Documents/BACKLOG-user-story.md).</summary>
public class ProductionPlanStageServiceTests
{
    private readonly Mock<IRepository<ProductionPlanStage>> _repositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<Stage>> _stageRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ProductionPlanStageService _sut;

    public ProductionPlanStageServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_repositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _sut = new ProductionPlanStageService(_unitOfWorkMock.Object);

        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionPlan { Id = 1, LineId = 1 });
        _stageRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object id, CancellationToken _) => new Stage { Id = (int)id, Name = $"Stage {id}", IsActive = true });
    }

    // AC1 — Thêm công đoạn vào kế hoạch: chưa có SequenceNumber -> thêm vào cuối danh sách (mặc định).
    [Fact]
    public async Task AddAsync_ChuaCoCongDoanNao_ThemVaoViTri1()
    {
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage>());

        var result = await _sut.AddAsync(1, new AddStageToProductionPlanRequest { StageId = 10 });

        Assert.Equal(1, result.SequenceNumber);
        Assert.Null(result.PreviousStageId);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ProductionPlanStage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_DaCo2CongDoan_ThemVaoCuoiDanhSachTheoMacDinh()
    {
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, ProductionPlanId = 1, StageId = 20, SequenceNumber = 2 },
        };
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

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
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, SequenceNumber = 1 },
        };
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.AddAsync(1, new AddStageToProductionPlanRequest { StageId = 10 }));
    }

    // AC4 — Từ chối khi trùng số thứ tự.
    [Fact]
    public async Task AddAsync_TrungSoThuTuDaChiDinh_NemBusinessRuleException()
    {
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, SequenceNumber = 1 },
        };
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.AddAsync(1, new AddStageToProductionPlanRequest { StageId = 20, SequenceNumber = 1 }));
    }

    // AC2 — Gỡ công đoạn khỏi kế hoạch: trình tự còn lại được đánh số lại liên tục.
    [Fact]
    public async Task RemoveAsync_GoCongDoanODau_ConLaiDuocDanhSoLaiLienTuc()
    {
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, ProductionPlanId = 1, StageId = 20, SequenceNumber = 2 },
            new() { Id = 3, ProductionPlanId = 1, StageId = 30, SequenceNumber = 3 },
        };
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _sut.RemoveAsync(1, 10);

        _repositoryMock.Verify(r => r.Remove(existing[0]), Times.Once);
        Assert.Equal(1, existing[1].SequenceNumber); // công đoạn 20 từ vị trí 2 -> 1
        Assert.Equal(2, existing[2].SequenceNumber); // công đoạn 30 từ vị trí 3 -> 2
    }

    [Fact]
    public async Task RemoveAsync_CongDoanKhongThuocKeHoach_NemEntityNotFoundException()
    {
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage>());

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.RemoveAsync(1, 999));
    }

    // AC3 — Sắp xếp lại trình tự: lưu đúng trình tự mới, tự xác định lại công đoạn liền trước.
    [Fact]
    public async Task ReorderAsync_HoanDoi2ViTri_LuuDungTrinhTuMoiVaSuyRaLienTruocDung()
    {
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, ProductionPlanId = 1, StageId = 20, SequenceNumber = 2 },
        };
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

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
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, ProductionPlanId = 1, StageId = 20, SequenceNumber = 2 },
        };
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

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
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, ProductionPlanId = 1, StageId = 20, SequenceNumber = 2 },
        };
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

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
}
