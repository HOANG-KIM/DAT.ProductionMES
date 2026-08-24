using ProductionMES.Application.DTOs.PackingModelConfigs;
using ProductionMES.Application.Validators.PackingModelConfigs;

namespace ProductionMES.Application.Tests.Validators;

/// <summary>Unit test cho CreatePackingModelConfigRequestValidator — AC7 của US-24 (ràng buộc dữ liệu bắt buộc).</summary>
public class CreatePackingModelConfigRequestValidatorTests
{
    private readonly CreatePackingModelConfigRequestValidator _sut = new();

    [Fact]
    public void Validate_DuLieuHopLe_HopLe()
    {
        var request = new CreatePackingModelConfigRequest
        {
            Model = "ABC-123",
            PackingQuantity = 20,
            GrossWeight = 5.5m,
            PartName = "Sản phẩm A",
            Manufacturer = "Nhà máy X",
        };

        var result = _sut.Validate(request);

        Assert.True(result.IsValid);
    }

    // AC7 — bỏ trống Model.
    [Fact]
    public void Validate_ModelRong_KhongHopLe()
    {
        var request = new CreatePackingModelConfigRequest { Model = "", PackingQuantity = 20, PartName = "Sản phẩm A" };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePackingModelConfigRequest.Model));
    }

    // AC7 — bỏ trống Tên sản phẩm.
    [Fact]
    public void Validate_TenSanPhamRong_KhongHopLe()
    {
        var request = new CreatePackingModelConfigRequest { Model = "ABC-123", PackingQuantity = 20, PartName = "" };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePackingModelConfigRequest.PartName));
    }

    // AC7 — Quy cách đóng gói <= 0.
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_QuyCachDongGoiKhongDuong_KhongHopLe(int packingQuantity)
    {
        var request = new CreatePackingModelConfigRequest { Model = "ABC-123", PackingQuantity = packingQuantity, PartName = "Sản phẩm A" };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePackingModelConfigRequest.PackingQuantity));
    }

    // AC7 — Khối lượng <= 0 (nếu có nhập).
    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void Validate_KhoiLuongKhongDuongKhiCoNhap_KhongHopLe(decimal grossWeight)
    {
        var request = new CreatePackingModelConfigRequest { Model = "ABC-123", PackingQuantity = 20, GrossWeight = grossWeight, PartName = "Sản phẩm A" };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePackingModelConfigRequest.GrossWeight));
    }

    // AC7 — Khối lượng để trống (không bắt buộc) vẫn hợp lệ.
    [Fact]
    public void Validate_KhoiLuongDeTrong_HopLe()
    {
        var request = new CreatePackingModelConfigRequest { Model = "ABC-123", PackingQuantity = 20, GrossWeight = null, PartName = "Sản phẩm A" };

        var result = _sut.Validate(request);

        Assert.True(result.IsValid);
    }
}
