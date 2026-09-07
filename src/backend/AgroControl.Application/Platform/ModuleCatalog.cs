using AgroControl.Domain.Platform;

namespace AgroControl.Application.Platform;

public sealed class ModuleCatalog : IModuleCatalog
{
    private static readonly IReadOnlyCollection<ModuleDefinition> Modules =
    [
        new(ModuleKey.Identity, "Identity", ModuleStatus.Active, "Usuários, acesso e autenticação."),
        new(ModuleKey.Organizations, "Organizations", ModuleStatus.Active, "Organizações e grupos empresariais."),
        new(ModuleKey.Farms, "Farms", ModuleStatus.Active, "Propriedades rurais."),
        new(ModuleKey.Fields, "Fields", ModuleStatus.Active, "Talhões e áreas produtivas."),
        new(ModuleKey.Crops, "Crops", ModuleStatus.Active, "Culturas agrícolas."),
        new(ModuleKey.Seasons, "Seasons", ModuleStatus.Active, "Safras e ciclos produtivos."),
        new(ModuleKey.Inventory, "Inventory", ModuleStatus.Active, "Estoque de insumos e materiais."),
        new(ModuleKey.Finance, "Finance", ModuleStatus.Active, "Custos, receitas e rentabilidade."),
        new(ModuleKey.Machinery, "Machinery", ModuleStatus.Active, "Máquinas, combustível e manutenção."),
        new(ModuleKey.Market, "Market", ModuleStatus.Active, "Mercado, preços e commodities."),
        new(ModuleKey.PrecisionAgriculture, "Precision Agriculture", ModuleStatus.Locked, "Mapas, drones e agricultura de precisão."),
        new(ModuleKey.Intelligence, "Agro Intelligence", ModuleStatus.Active, "Análise de dados, previsão e IA."),
        new(ModuleKey.Irrigation, "Irrigation", ModuleStatus.Locked, "Irrigação, clima e umidade."),
        new(ModuleKey.Sustainability, "Sustainability", ModuleStatus.Locked, "Indicadores ambientais e carbono."),
        new(ModuleKey.Export, "Export", ModuleStatus.Locked, "Comércio exterior, câmbio e logística."),
        new(ModuleKey.Telemetry, "Telemetry", ModuleStatus.Locked, "Sensores, GPS, máquinas e IoT.")
    ];

    public IReadOnlyCollection<ModuleDefinition> GetAll() => Modules;
}
