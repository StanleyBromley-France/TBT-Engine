namespace Setup.Build.TemplateRegistry;

using Setup.Build.TemplateRegistry.Results;
using Setup.Config;
using Setup.Loading;
using Setup.Validation.Primitives;

public interface ITemplateRegistryBuilder
{
    TemplateRegistryBuildResult Build(
        ContentPackTemplates packTemplates,
        ContentValidationMode mode);
}
