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
        new(ModuleKey.PrecisionAgriculture, "Precision Agriculture", ModuleStatus.Active, "Talhões georreferenciados, mapas e agricultura de precisão."),
        new(ModuleKey.Intelligence, "Agro Intelligence", ModuleStatus.Active, "Análise de dados, previsão e IA."),
        new(ModuleKey.Irrigation, "Irrigation", ModuleStatus.Active, "Zonas de irrigação, umidade do solo e manejo hídrico."),
        new(ModuleKey.Sustainability, "Sustainability", ModuleStatus.Active, "Emissões estimadas, CO₂e e indicadores ambientais."),
        new(ModuleKey.Commercial, "Commercial", ModuleStatus.Active, "Clientes, contatos, oportunidades e pipeline comercial."),
        new(ModuleKey.Export, "Export", ModuleStatus.Active, "Pedidos internacionais, câmbio, documentos e logística."),
        new(ModuleKey.Telemetry, "Telemetry", ModuleStatus.Active, "Sensores, GPS, máquinas e IoT.")
    ];

    public IReadOnlyCollection<ModuleDefinition> GetAll() => Modules;
}
