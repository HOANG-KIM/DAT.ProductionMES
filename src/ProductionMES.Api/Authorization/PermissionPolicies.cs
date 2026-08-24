namespace ProductionMES.Api.Authorization;

/// <summary>
/// Hằng số tên policy dạng <c>"{Resource}.{Action}"</c> (ADR-004) — tránh magic string rải rác ở Controller,
/// và tránh gõ sai tên không khớp giữa policy đăng ký ở <c>Program.cs</c> và attribute
/// <c>[Authorize(Policy = ...)]</c> dùng ở Controller. Khớp chính xác catalog seed ở <c>DbSeeder.SeedPermissionsAsync</c>
/// (tổng 34 permission: Line/Stage/WorkStation 4 action x 3 resource = 12, ProductionPlan 3, ProductionPlanStage 7
/// (US-05a bổ sung Apply/Pause/Close, thay cho Activate/Deactivate cấp cả kế hoạch trước đây), BreakWindow 4,
/// StationApiKey 4, Scan 3 (View — US-10, tra cứu lịch sử scan; ConfirmNg — US-18, thay đổi 18/08/2026, xác nhận Scan NG;
/// ReworkUnlock — US-19, mở khóa rework), Report 1 (View — US-21, báo cáo tổng hợp theo Line/công đoạn)).
/// </summary>
public static class PermissionPolicies
{
    public const string LineView = "Line.View";
    public const string LineCreate = "Line.Create";
    public const string LineUpdate = "Line.Update";
    public const string LineDeactivate = "Line.Deactivate";

    public const string BreakWindowView = "BreakWindow.View";
    public const string BreakWindowCreate = "BreakWindow.Create";
    public const string BreakWindowUpdate = "BreakWindow.Update";
    public const string BreakWindowDelete = "BreakWindow.Delete";

    // US-04a: View = GetCurrent (AC2), Create = Issue (AC1), Update = Reissue (AC4), Deactivate = Revoke (AC3).
    public const string StationApiKeyView = "StationApiKey.View";
    public const string StationApiKeyCreate = "StationApiKey.Create";
    public const string StationApiKeyUpdate = "StationApiKey.Update";
    public const string StationApiKeyDeactivate = "StationApiKey.Deactivate";

    public const string StageView = "Stage.View";
    public const string StageCreate = "Stage.Create";
    public const string StageUpdate = "Stage.Update";
    public const string StageDeactivate = "Stage.Deactivate";

    public const string WorkStationView = "WorkStation.View";
    public const string WorkStationCreate = "WorkStation.Create";
    public const string WorkStationUpdate = "WorkStation.Update";
    public const string WorkStationDeactivate = "WorkStation.Deactivate";

    public const string ProductionPlanView = "ProductionPlan.View";
    public const string ProductionPlanCreate = "ProductionPlan.Create";
    public const string ProductionPlanUpdate = "ProductionPlan.Update";

    public const string ProductionPlanStageView = "ProductionPlanStage.View";
    public const string ProductionPlanStageCreate = "ProductionPlanStage.Create";
    public const string ProductionPlanStageUpdate = "ProductionPlanStage.Update";
    public const string ProductionPlanStageDelete = "ProductionPlanStage.Delete";

    // US-05a: vòng đời trạng thái theo cặp (Kế hoạch, Công đoạn) — Apply = Áp dụng (AC1), Pause = Tạm dừng (AC3),
    // Close = Đóng kế hoạch (AC6).
    public const string ProductionPlanStageApply = "ProductionPlanStage.Apply";
    public const string ProductionPlanStagePause = "ProductionPlanStage.Pause";
    public const string ProductionPlanStageClose = "ProductionPlanStage.Close";

    // US-10 AC2/AC3: tra cứu lịch sử scan — Supervisor/Admin/Manager (KHÔNG dùng scheme StationApiKey).
    public const string ScanView = "Scan.View";

    // US-18 (thay đổi yêu cầu 18/08/2026): xác nhận Scan NG tại trạm — Supervisor/Admin/Manager, yêu cầu đăng
    // nhập lại (re-auth) mỗi lần kích hoạt Chế độ Scan NG (KHÔNG dùng scheme StationApiKey).
    public const string ScanConfirmNg = "Scan.ConfirmNg";

    // US-19 AC2/AC6: "Mở khóa rework" cho tem bị NG — chỉ Supervisor/Admin (KHÔNG cấp Manager, khác ScanConfirmNg).
    public const string ScanReworkUnlock = "Scan.ReworkUnlock";

    // US-21: báo cáo tổng hợp ACTUAL/PLAN/BALANCE theo Line/công đoạn — Supervisor/Admin/Manager, cùng đối tượng với ScanView.
    public const string ReportView = "Report.View";

    // US-24: cấu hình Quy cách đóng gói theo Model — Admin (web-admin) + Supervisor (Station.Wpf nâng quyền), AC6.
    public const string PackingModelConfigView = "PackingModelConfig.View";
    public const string PackingModelConfigCreate = "PackingModelConfig.Create";
    public const string PackingModelConfigUpdate = "PackingModelConfig.Update";

    // US-25 AC7/AC8: thao tác Supervisor tại "Đóng thùng" — Admin + Supervisor (KHÔNG Manager, cùng phạm vi ScanReworkUnlock).
    public const string PackingBoxUpdate = "PackingBox.Update";
    public const string PackingBoxConfirmDuplicate = "PackingBox.ConfirmDuplicate";
}
