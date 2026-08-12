namespace ProductionMES.Api.Authorization;

/// <summary>
/// Hằng số tên policy dạng <c>"{Resource}.{Action}"</c> (ADR-004) — tránh magic string rải rác ở Controller,
/// và tránh gõ sai tên không khớp giữa policy đăng ký ở <c>Program.cs</c> và attribute
/// <c>[Authorize(Policy = ...)]</c> dùng ở Controller. Khớp chính xác catalog seed ở <c>DbSeeder.SeedPermissionsAsync</c>
/// (tổng 21 permission: Line/Stage/WorkStation 4 action x 3 resource = 12, ProductionPlan 5, ProductionPlanStage 4).
/// </summary>
public static class PermissionPolicies
{
    public const string LineView = "Line.View";
    public const string LineCreate = "Line.Create";
    public const string LineUpdate = "Line.Update";
    public const string LineDeactivate = "Line.Deactivate";

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
    public const string ProductionPlanActivate = "ProductionPlan.Activate";
    public const string ProductionPlanDeactivate = "ProductionPlan.Deactivate";

    public const string ProductionPlanStageView = "ProductionPlanStage.View";
    public const string ProductionPlanStageCreate = "ProductionPlanStage.Create";
    public const string ProductionPlanStageUpdate = "ProductionPlanStage.Update";
    public const string ProductionPlanStageDelete = "ProductionPlanStage.Delete";
}
