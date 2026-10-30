using Microsoft.Extensions.Logging;

namespace YongAnGame.ModuleLib.Features.Configs;

/// <summary>
/// <see cref="ConfigRepository"/> 的日志定义，由 LoggerMessage 源生成器生成实现
/// </summary>
public partial class ConfigRepository
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "正在尝试注册{ConfigId}到{ConfigTypeName}类型")]
    private static partial void LogLoadingConfig(ILogger logger, string configId, string configTypeName);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "无法注册{ConfigId}到{ConfigTypeName}类型，因为指定的{ConfigTypeName}类型ID无效")]
    private static partial void LogInvalidConfigId(ILogger logger, string configId, string configTypeName);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "无法注册{ConfigId}到{ConfigTypeName}类型，因为读取文件失败：{Path}")]
    private static partial void LogReadFileFailed(ILogger logger, Exception ex, string configId, string configTypeName, string path);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Error,
        Message = "无法注册{ConfigId}到{ConfigTypeName}类型，因为配置数据无效：{Path}")]
    private static partial void LogDeserializeFailed(ILogger logger, Exception ex, string configId, string configTypeName, string path);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Error,
        Message = "无法注册{ConfigId}到{ConfigTypeName}类型，因为指定的{ConfigTypeName}类型数据无效")]
    private static partial void LogInvalidConfigData(ILogger logger, string configId, string configTypeName);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Warning,
        Message = "配置{ConfigId}已在{ConfigTypeName}类型中注册，跳过重复注册")]
    private static partial void LogDuplicateRegistration(ILogger logger, string configId, string configTypeName);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Information,
        Message = "完成注册{ConfigId}到{ConfigTypeName}类型")]
    private static partial void LogRegistrationCompleted(ILogger logger, string configId, string configTypeName);

    [LoggerMessage(
    EventId = 1008,
    Level = LogLevel.Information,
    Message = "已卸载{ConfigId}从{ConfigTypeName}类型")]
    private static partial void LogConfigUnloaded(ILogger logger, string configId, string configTypeName);

    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Debug,
        Message = "配置{ConfigId}未在{ConfigTypeName}类型中注册，无需卸载")]
    private static partial void LogConfigNotLoaded(ILogger logger, string configId, string configTypeName);

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Information,
        Message = "已卸载{ConfigTypeName}类型的全部配置，共{Count}项")]
    private static partial void LogConfigGroupUnloaded(ILogger logger, string configTypeName, int count);

    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Information,
        Message = "已卸载全部配置，共{Count}项")]
    private static partial void LogAllConfigsUnloaded(ILogger logger, int count);

    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Warning,
        Message = "尝试卸载未注册的{ConfigTypeName}类型")]
    private static partial void LogUnloadUnknownGroup(ILogger logger, string configTypeName);
}