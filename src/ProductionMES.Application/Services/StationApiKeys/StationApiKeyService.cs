using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Security;
using ProductionMES.Application.DTOs.StationApiKeys;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Services.StationApiKeys;

/// <summary>
/// Implementation IStationApiKeyService (US-04a, ADR-005). Giá trị thô của API key KHÔNG BAO GIỜ được lưu lại
/// hay trả ra ngoài <see cref="IssueAsync"/>/<see cref="ReissueAsync"/> (AC1/AC2) — chỉ <see cref="ApiKeyGenerator.HashApiKey"/>
/// (SHA-256 hex, cùng nguyên tắc <c>RefreshToken.TokenHash</c>) được lưu ở <see cref="StationApiKey.KeyHash"/>.
/// </summary>
public class StationApiKeyService : IStationApiKeyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApiKeyGenerator _apiKeyGenerator;

    public StationApiKeyService(IUnitOfWork unitOfWork, IApiKeyGenerator apiKeyGenerator)
    {
        _unitOfWork = unitOfWork;
        _apiKeyGenerator = apiKeyGenerator;
    }

    public async Task<IssuedStationApiKeyDto> IssueAsync(int workStationId, CancellationToken cancellationToken = default)
    {
        await EnsureWorkStationExistsAsync(workStationId, cancellationToken);

        var repository = _unitOfWork.Repository<StationApiKey>();
        var existingItems = await repository.FindAsync(x => x.WorkStationId == workStationId, cancellationToken);

        // AC1: cấp mới chỉ áp dụng cho trạm CHƯA có key Active — nếu đang có, phải dùng ReissueAsync (xoay
        // vòng) để đảm bảo luôn tối đa 1 key Active tại 1 thời điểm cho mỗi trạm và không "mồ côi" key cũ.
        if (existingItems.Any(x => x.RevokedAtUtc is null))
        {
            throw new BusinessRuleException(
                $"Trạm Id = {workStationId} đang có 1 API Key Active — dùng chức năng cấp lại (reissue) để xoay vòng, không cấp mới trùng.");
        }

        return await CreateNewKeyAsync(workStationId, cancellationToken);
    }

    public async Task RevokeAsync(int workStationId, CancellationToken cancellationToken = default)
    {
        await EnsureWorkStationExistsAsync(workStationId, cancellationToken);

        var repository = _unitOfWork.Repository<StationApiKey>();
        var existingItems = await repository.FindAsync(x => x.WorkStationId == workStationId, cancellationToken);
        var activeKey = existingItems.FirstOrDefault(x => x.RevokedAtUtc is null);

        // AC3: chỉ thu hồi được khi trạm đang có 1 key Active.
        if (activeKey is null)
        {
            throw new BusinessRuleException($"Trạm Id = {workStationId} không có API Key nào đang Active để thu hồi.");
        }

        activeKey.RevokedAtUtc = DateTime.UtcNow;
        repository.Update(activeKey);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IssuedStationApiKeyDto> ReissueAsync(int workStationId, CancellationToken cancellationToken = default)
    {
        await EnsureWorkStationExistsAsync(workStationId, cancellationToken);

        var repository = _unitOfWork.Repository<StationApiKey>();
        var existingItems = await repository.FindAsync(x => x.WorkStationId == workStationId, cancellationToken);
        var activeKey = existingItems.FirstOrDefault(x => x.RevokedAtUtc is null);

        // AC4: key cũ (nếu có) tự động chuyển Revoked, giữ nguyên bản ghi (không xóa) để giữ lịch sử truy vết.
        var nowUtc = DateTime.UtcNow;
        if (activeKey is not null)
        {
            activeKey.RevokedAtUtc = nowUtc;
            repository.Update(activeKey);
        }

        return await CreateNewKeyAsync(workStationId, cancellationToken, nowUtc);
    }

    public async Task<StationApiKeyDto?> GetCurrentAsync(int workStationId, CancellationToken cancellationToken = default)
    {
        await EnsureWorkStationExistsAsync(workStationId, cancellationToken);

        var existingItems = await _unitOfWork.Repository<StationApiKey>()
            .FindAsync(x => x.WorkStationId == workStationId, cancellationToken);

        // AC2: metadata của key mới nhất (Active hoặc đã Revoked) — không hiển thị lại giá trị thô.
        var latest = existingItems.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
        return latest is null ? null : ToDto(latest);
    }

    public async Task<int?> ValidateAsync(string rawApiKey, int? expectedWorkStationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            return null;
        }

        var keyHash = _apiKeyGenerator.HashApiKey(rawApiKey);
        var matches = await _unitOfWork.Repository<StationApiKey>().FindAsync(x => x.KeyHash == keyHash, cancellationToken);
        var stationApiKey = matches.FirstOrDefault();

        // AC5: key không tồn tại hoặc đã bị thu hồi -> từ chối.
        if (stationApiKey is null || stationApiKey.RevokedAtUtc is not null)
        {
            return null;
        }

        // AC6: key hợp lệ nhưng thuộc về trạm khác trạm gửi trong request -> từ chối (chống giả danh).
        if (expectedWorkStationId is not null && expectedWorkStationId.Value != stationApiKey.WorkStationId)
        {
            return null;
        }

        return stationApiKey.WorkStationId;
    }

    private async Task<IssuedStationApiKeyDto> CreateNewKeyAsync(int workStationId, CancellationToken cancellationToken, DateTime? createdAtUtc = null)
    {
        var rawApiKey = _apiKeyGenerator.GenerateApiKey();
        var stationApiKey = new StationApiKey
        {
            WorkStationId = workStationId,
            KeyHash = _apiKeyGenerator.HashApiKey(rawApiKey),
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
        };

        await _unitOfWork.Repository<StationApiKey>().AddAsync(stationApiKey, cancellationToken);
        // SaveChanges phải chạy TRƯỚC khi đọc stationApiKey.Id: AddAsync chỉ track entity, Id (identity column)
        // chỉ được DB gán giá trị thật sau khi SaveChangesAsync round-trip xong.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssuedStationApiKeyDto
        {
            Id = stationApiKey.Id,
            WorkStationId = stationApiKey.WorkStationId,
            CreatedAtUtc = stationApiKey.CreatedAtUtc,
            ApiKey = rawApiKey,
        };
    }

    private async Task EnsureWorkStationExistsAsync(int workStationId, CancellationToken cancellationToken)
    {
        var workStation = await _unitOfWork.Repository<WorkStation>().GetByIdAsync(workStationId, cancellationToken);
        if (workStation is null)
        {
            throw new EntityNotFoundException($"Không tìm thấy trạm làm việc với Id = {workStationId}.");
        }
    }

    private static StationApiKeyDto ToDto(StationApiKey stationApiKey) => new()
    {
        Id = stationApiKey.Id,
        WorkStationId = stationApiKey.WorkStationId,
        CreatedAtUtc = stationApiKey.CreatedAtUtc,
        RevokedAtUtc = stationApiKey.RevokedAtUtc,
    };
}
