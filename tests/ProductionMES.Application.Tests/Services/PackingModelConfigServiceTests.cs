using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Storage;
using ProductionMES.Application.DTOs.PackingModelConfigs;
using ProductionMES.Application.Services.PackingModelConfigs;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho PackingModelConfigService, bám theo AC1-AC5/AC9 của US-24 (Documents/BACKLOG-user-story.md).
/// AC7 (validate bắt buộc) được test riêng ở tầng FluentValidation (xem
/// <c>Validators/CreatePackingModelConfigRequestValidatorTests</c>), không lặp lại ở đây.
/// </summary>
public class PackingModelConfigServiceTests
{
    private readonly Mock<IRepository<PackingModelConfig>> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPackingTemplateStorage> _templateStorageMock = new();
    private readonly PackingModelConfigService _sut;

    public PackingModelConfigServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<PackingModelConfig>()).Returns(_repositoryMock.Object);
        _sut = new PackingModelConfigService(_unitOfWorkMock.Object, _templateStorageMock.Object);
    }

    // AC1 — Thêm cấu hình đóng gói mới cho 1 Model chưa có cấu hình -> tạo mới thành công.
    [Fact]
    public async Task CreateAsync_ModelChuaCoCauHinh_TaoMoiThanhCong()
    {
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PackingModelConfig, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PackingModelConfig>());

        var request = new CreatePackingModelConfigRequest
        {
            Model = "ABC-123",
            PackingQuantity = 20,
            GrossWeight = 5.5m,
            PartName = "Sản phẩm A",
            Manufacturer = "Nhà máy X",
        };

        var result = await _sut.CreateAsync(request, "supervisor01");

        Assert.Equal("ABC-123", result.Model);
        Assert.Equal(20, result.PackingQuantity);
        Assert.False(result.HasTemplate);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<PackingModelConfig>(c => c.Model == "ABC-123" && c.ModelNormalized == "ABC-123"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC9 — So khớp Model không phân biệt hoa/thường, tự trim khoảng trắng -> Create bị từ chối nếu đã tồn tại (dù lệch hoa/thường/khoảng trắng).
    [Fact]
    public async Task CreateAsync_ModelDaCoCauHinhLechHoaThuongVaKhoangTrang_NemBusinessRuleException()
    {
        var existing = new PackingModelConfig { Id = 1, Model = "ABC-123", ModelNormalized = "ABC-123", PackingQuantity = 10, PartName = "Sản phẩm A" };
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PackingModelConfig, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PackingModelConfig> { existing });

        var request = new CreatePackingModelConfigRequest { Model = " abc-123 ", PackingQuantity = 20, PartName = "Sản phẩm B" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request, "supervisor01"));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<PackingModelConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC2 — Sửa cấu hình đã có: cập nhật quy cách/khối lượng/tên sản phẩm/nhà sản xuất, Model giữ nguyên.
    [Fact]
    public async Task UpdateAsync_CauHinhDaTonTai_CapNhatThanhCongVaKhongDoiModel()
    {
        var existing = new PackingModelConfig
        {
            Id = 1,
            Model = "ABC-123",
            ModelNormalized = "ABC-123",
            PackingQuantity = 10,
            PartName = "Sản phẩm cũ",
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var request = new UpdatePackingModelConfigRequest { PackingQuantity = 30, GrossWeight = 8.2m, PartName = "Sản phẩm mới", Manufacturer = "NM mới" };
        var result = await _sut.UpdateAsync(1, request, "supervisor02");

        Assert.Equal("ABC-123", result.Model); // Model không đổi
        Assert.Equal(30, result.PackingQuantity);
        Assert.Equal(8.2m, result.GrossWeight);
        Assert.Equal("Sản phẩm mới", result.PartName);
        Assert.Equal("NM mới", result.Manufacturer);
        Assert.Equal("supervisor02", result.UpdatedByUserName);
        _repositoryMock.Verify(r => r.Update(existing), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_KhongTonTai_NemEntityNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((PackingModelConfig?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _sut.UpdateAsync(99, new UpdatePackingModelConfigRequest { PackingQuantity = 1, PartName = "X" }, null));
    }

    // AC9 — Tra cứu theo Model không phân biệt hoa/thường, tự trim khoảng trắng.
    [Fact]
    public async Task GetByModelAsync_ModelLechHoaThuongVaKhoangTrang_VanKhopDungCauHinh()
    {
        var existing = new PackingModelConfig { Id = 1, Model = "Xyz-999", ModelNormalized = "XYZ-999", PackingQuantity = 15, PartName = "Sản phẩm" };
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PackingModelConfig, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PackingModelConfig> { existing });

        var result = await _sut.GetByModelAsync("  xyz-999  ");

        Assert.NotNull(result);
        Assert.Equal("Xyz-999", result!.Model);
    }

    [Fact]
    public async Task GetByModelAsync_ChuaTungCoCauHinh_TraVeNull()
    {
        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PackingModelConfig, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PackingModelConfig>());

        var result = await _sut.GetByModelAsync("KHONG-TON-TAI");

        Assert.Null(result);
    }

    // AC4 — Tải lên file mẫu tem hợp lệ (.xlsx) -> lưu file, đánh dấu HasTemplate, ghi nhận người/thời điểm.
    [Fact]
    public async Task UploadTemplateAsync_FileXlsxHopLe_LuuThanhCongVaDanhDauHasTemplate()
    {
        var existing = new PackingModelConfig { Id = 1, Model = "ABC-123", ModelNormalized = "ABC-123", PackingQuantity = 10, PartName = "Sản phẩm" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        using var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await _sut.UploadTemplateAsync(1, content, "mau-tem.xlsx", "supervisor01");

        Assert.True(result.HasTemplate);
        Assert.Equal("supervisor01", result.TemplateUpdatedByUserName);
        Assert.NotNull(result.TemplateUpdatedAtUtc);
        _templateStorageMock.Verify(s => s.SaveAsync(1, content, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC4 (chỉ nhận đúng phần mở rộng .xlsx) — sai định dạng -> từ chối, không lưu file.
    [Theory]
    [InlineData("mau-tem.doc")]
    [InlineData("mau-tem.pdf")]
    [InlineData("mau-tem")]
    public async Task UploadTemplateAsync_SaiDinhDangFile_NemBusinessRuleExceptionVaKhongLuu(string fileName)
    {
        var existing = new PackingModelConfig { Id = 1, Model = "ABC-123", ModelNormalized = "ABC-123", PackingQuantity = 10, PartName = "Sản phẩm" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        using var content = new MemoryStream(new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UploadTemplateAsync(1, content, fileName, "supervisor01"));
        _templateStorageMock.Verify(s => s.SaveAsync(It.IsAny<int>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadTemplateAsync_CauHinhKhongTonTai_NemEntityNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((PackingModelConfig?)null);
        using var content = new MemoryStream(new byte[] { 1 });

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.UploadTemplateAsync(99, content, "mau-tem.xlsx", null));
    }

    // AC5 — Tải xuống đúng file đã tải lên gần nhất.
    [Fact]
    public async Task DownloadTemplateAsync_DaCoTemplate_TraVeStreamDungFile()
    {
        var existing = new PackingModelConfig { Id = 1, Model = "ABC-123", ModelNormalized = "ABC-123", PackingQuantity = 10, PartName = "Sản phẩm", HasTemplate = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        using var expectedStream = new MemoryStream(new byte[] { 9, 9 });
        _templateStorageMock.Setup(s => s.OpenReadAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(expectedStream);

        var (content, fileName) = await _sut.DownloadTemplateAsync(1);

        Assert.Same(expectedStream, content);
        Assert.Contains("ABC-123", fileName);
        Assert.EndsWith(".xlsx", fileName);
    }

    [Fact]
    public async Task DownloadTemplateAsync_ChuaTungTaiLenTemplate_NemEntityNotFoundException()
    {
        var existing = new PackingModelConfig { Id = 1, Model = "ABC-123", ModelNormalized = "ABC-123", PackingQuantity = 10, PartName = "Sản phẩm", HasTemplate = false };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.DownloadTemplateAsync(1));
        _templateStorageMock.Verify(s => s.OpenReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC3 — Xem danh sách cấu hình theo Model.
    [Fact]
    public async Task GetAllAsync_CoNhieuCauHinh_TraVeDungDanhSachSapXepTheoModel()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PackingModelConfig>
        {
            new() { Id = 2, Model = "ZZZ", ModelNormalized = "ZZZ", PackingQuantity = 5, PartName = "B" },
            new() { Id = 1, Model = "AAA", ModelNormalized = "AAA", PackingQuantity = 5, PartName = "A" },
        });

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("AAA", result[0].Model);
        Assert.Equal("ZZZ", result[1].Model);
    }

    // AC9 — Autocomplete gợi ý Model đã có cấu hình.
    [Fact]
    public async Task SuggestModelsAsync_CoTuKhoaTimKiem_TraVeDungModelKhopGanDung()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PackingModelConfig>
        {
            new() { Id = 1, Model = "ABC-123", ModelNormalized = "ABC-123", PackingQuantity = 5, PartName = "A" },
            new() { Id = 2, Model = "XYZ-999", ModelNormalized = "XYZ-999", PackingQuantity = 5, PartName = "B" },
        });

        var result = await _sut.SuggestModelsAsync("abc");

        Assert.Single(result);
        Assert.Equal("ABC-123", result[0]);
    }
}
