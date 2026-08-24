namespace ProductionMES.Application.Services.PackingBoxes;

/// <summary>Kết quả tăng đếm 1 thùng sau 1 lượt scan Ok tại "Đóng thùng" (<see cref="IPackingBoxService.RegisterOkScanAsync"/>) — <c>ScanService</c> map các field này vào <c>ScanResultDto</c> trả về client.</summary>
public class PackingScanOutcome
{
    public int BoxId { get; set; }

    public int BoxNo { get; set; }

    public int ScannedQuantity { get; set; }

    public int TargetQuantity { get; set; }

    /// <summary>AC4: true nếu ĐÚNG lượt scan này vừa làm đủ số lượng.</summary>
    public bool BoxCompleted { get; set; }

    /// <summary>Id thùng vừa hoàn tất — chỉ có giá trị khi <see cref="BoxCompleted"/> = true (dùng cho AC4 tự động in tem).</summary>
    public int? CompletedBoxId { get; set; }
}
