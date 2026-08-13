using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Security;
using ProductionMES.Application.Services.StationApiKeys;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>Unit test cho StationApiKeyService, bám theo AC1-AC6 của US-04a (Documents/BACKLOG-user-story.md).</summary>
public class StationApiKeyServiceTests
{
    private readonly Mock<IRepository<StationApiKey>> _stationApiKeyRepositoryMock = new();
    private readonly Mock<IRepository<WorkStation>> _workStationRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IApiKeyGenerator> _apiKeyGeneratorMock = new();
    private readonly StationApiKeyService _sut;

    public StationApiKeyServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<StationApiKey>()).Returns(_stationApiKeyRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _sut = new StationApiKeyService(_unitOfWorkMock.Object, _apiKeyGeneratorMock.Object);

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, Name = "Trạm 1", IsActive = true });

        _apiKeyGeneratorMock.Setup(g => g.GenerateApiKey()).Returns("raw-api-key-value");
        _apiKeyGeneratorMock.Setup(g => g.HashApiKey(It.IsAny<string>()))
            .Returns((string raw) => $"hash-of-{raw}");
    }

    // AC1 — Cấp API Key cho trạm: trạm chưa có key nào -> sinh key, trả giá trị thô đúng 1 lần, chỉ lưu hash.
    [Fact]
    public async Task IssueAsync_TramChuaCoKeyNao_SinhKeyMoiVaTraGiaTriThoDungMotLan()
    {
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey>());

        var result = await _sut.IssueAsync(1);

        Assert.Equal(1, result.WorkStationId);
        Assert.Equal("raw-api-key-value", result.ApiKey);
        _stationApiKeyRepositoryMock.Verify(
            r => r.AddAsync(It.Is<StationApiKey>(k => k.WorkStationId == 1 && k.KeyHash == "hash-of-raw-api-key-value" && k.RevokedAtUtc == null), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC1 (ràng buộc bổ sung) — trạm đang có key Active -> từ chối cấp mới, hướng dẫn dùng reissue.
    [Fact]
    public async Task IssueAsync_TramDangCoKeyActive_NemBusinessRuleException()
    {
        var activeKey = new StationApiKey { Id = 1, WorkStationId = 1, KeyHash = "abc", CreatedAtUtc = DateTime.UtcNow, RevokedAtUtc = null };
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey> { activeKey });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.IssueAsync(1));
        _stationApiKeyRepositoryMock.Verify(r => r.AddAsync(It.IsAny<StationApiKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IssueAsync_TramKhongTonTai_NemEntityNotFoundException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((WorkStation?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.IssueAsync(99));
    }

    // AC3 — Thu hồi API Key: trạm đang có key Active -> chuyển Revoked, ghi nhận thời điểm thu hồi.
    [Fact]
    public async Task RevokeAsync_TramDangCoKeyActive_ChuyenRevokedVaGhiNhanThoiDiem()
    {
        var activeKey = new StationApiKey { Id = 1, WorkStationId = 1, KeyHash = "abc", CreatedAtUtc = DateTime.UtcNow, RevokedAtUtc = null };
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey> { activeKey });

        await _sut.RevokeAsync(1);

        Assert.NotNull(activeKey.RevokedAtUtc);
        _stationApiKeyRepositoryMock.Verify(r => r.Update(activeKey), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC3 (ràng buộc bổ sung) — trạm không có key Active -> từ chối thu hồi.
    [Fact]
    public async Task RevokeAsync_TramKhongCoKeyActive_NemBusinessRuleException()
    {
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey>());

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.RevokeAsync(1));
    }

    // AC4 — Xoay vòng: key cũ tự động Revoked, key mới có hiệu lực, lịch sử key cũ vẫn giữ lại (không xóa).
    [Fact]
    public async Task ReissueAsync_TramDangCoKeyActive_RevokeKeyCuVaTaoKeyMoiGiuLaiLichSu()
    {
        var oldKey = new StationApiKey { Id = 1, WorkStationId = 1, KeyHash = "old-hash", CreatedAtUtc = DateTime.UtcNow.AddDays(-30), RevokedAtUtc = null };
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey> { oldKey });

        var result = await _sut.ReissueAsync(1);

        Assert.NotNull(oldKey.RevokedAtUtc); // key cũ chuyển Revoked
        Assert.Equal("raw-api-key-value", result.ApiKey); // key mới trả giá trị thô đúng 1 lần
        _stationApiKeyRepositoryMock.Verify(r => r.Update(oldKey), Times.Once); // giữ lại bản ghi cũ, chỉ update trạng thái
        _stationApiKeyRepositoryMock.Verify(r => r.Remove(It.IsAny<StationApiKey>()), Times.Never); // không xóa lịch sử
        _stationApiKeyRepositoryMock.Verify(r => r.AddAsync(It.IsAny<StationApiKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC4 — Xoay vòng khi trạm chưa từng có key nào vẫn hoạt động bình thường (không có key cũ để revoke).
    [Fact]
    public async Task ReissueAsync_TramChuaTungCoKeyNao_TaoKeyMoiKhongLoi()
    {
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey>());

        var result = await _sut.ReissueAsync(1);

        Assert.Equal("raw-api-key-value", result.ApiKey);
        _stationApiKeyRepositoryMock.Verify(r => r.Update(It.IsAny<StationApiKey>()), Times.Never);
    }

    // AC2 — GetCurrentAsync chỉ trả metadata, không có giá trị thô hay hash trong DTO (kiểm tra bằng type — StationApiKeyDto không có field ApiKey/KeyHash).
    [Fact]
    public async Task GetCurrentAsync_CoKeyDaCap_TraVeMetadataDungTrangThai()
    {
        var revokedKey = new StationApiKey { Id = 1, WorkStationId = 1, KeyHash = "old", CreatedAtUtc = DateTime.UtcNow.AddDays(-10), RevokedAtUtc = DateTime.UtcNow.AddDays(-5) };
        var activeKey = new StationApiKey { Id = 2, WorkStationId = 1, KeyHash = "new", CreatedAtUtc = DateTime.UtcNow, RevokedAtUtc = null };
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey> { revokedKey, activeKey });

        var result = await _sut.GetCurrentAsync(1);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Id); // key mới nhất
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetCurrentAsync_TramChuaTungCapKeyNao_TraVeNull()
    {
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey>());

        var result = await _sut.GetCurrentAsync(1);

        Assert.Null(result);
    }

    // AC5 — Xác thực API key hợp lệ, chưa bị thu hồi -> trả về đúng WorkStationId.
    [Fact]
    public async Task ValidateAsync_KeyHopLeChuaBiThuHoi_TraVeWorkStationId()
    {
        var activeKey = new StationApiKey { Id = 1, WorkStationId = 5, KeyHash = "hash-of-valid-raw-key", CreatedAtUtc = DateTime.UtcNow, RevokedAtUtc = null };
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey> { activeKey });

        var result = await _sut.ValidateAsync("valid-raw-key", expectedWorkStationId: null);

        Assert.Equal(5, result);
    }

    // AC5 — Từ chối khi key không tồn tại (hash không khớp bất kỳ bản ghi nào).
    [Fact]
    public async Task ValidateAsync_KeyKhongTonTai_TraVeNull()
    {
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey>());

        var result = await _sut.ValidateAsync("khong-ton-tai", expectedWorkStationId: null);

        Assert.Null(result);
    }

    // AC5 — Từ chối khi key đã bị thu hồi.
    [Fact]
    public async Task ValidateAsync_KeyDaBiThuHoi_TraVeNull()
    {
        var revokedKey = new StationApiKey { Id = 1, WorkStationId = 5, KeyHash = "hash-of-revoked-raw-key", CreatedAtUtc = DateTime.UtcNow.AddDays(-1), RevokedAtUtc = DateTime.UtcNow };
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey> { revokedKey });

        var result = await _sut.ValidateAsync("revoked-raw-key", expectedWorkStationId: null);

        Assert.Null(result);
    }

    // AC6 — Key hợp lệ nhưng thuộc về WorkStationId khác với request -> từ chối (chống giả danh).
    [Fact]
    public async Task ValidateAsync_KeyHopLeNhungSaiTram_TraVeNull()
    {
        var stationAKey = new StationApiKey { Id = 1, WorkStationId = 1, KeyHash = "hash-of-station-a-key", CreatedAtUtc = DateTime.UtcNow, RevokedAtUtc = null };
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey> { stationAKey });

        // Trạm A dùng key của mình nhưng request khai báo WorkStationId = 2 (trạm B) -> phải bị từ chối.
        var result = await _sut.ValidateAsync("station-a-key", expectedWorkStationId: 2);

        Assert.Null(result);
    }

    // AC6 (đối chứng) — Key hợp lệ và đúng trạm gửi trong request -> chấp nhận.
    [Fact]
    public async Task ValidateAsync_KeyHopLeVaDungTram_TraVeWorkStationId()
    {
        var stationAKey = new StationApiKey { Id = 1, WorkStationId = 1, KeyHash = "hash-of-station-a-key", CreatedAtUtc = DateTime.UtcNow, RevokedAtUtc = null };
        _stationApiKeyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StationApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StationApiKey> { stationAKey });

        var result = await _sut.ValidateAsync("station-a-key", expectedWorkStationId: 1);

        Assert.Equal(1, result);
    }
}
