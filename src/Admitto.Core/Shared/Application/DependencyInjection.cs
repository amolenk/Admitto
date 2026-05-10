using Amolenk.Admitto.Core.Shared.Application.Cryptography;
using FluentValidation;
using FluentValidation.Internal;
using Humanizer;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class SharedApplicationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCryptographyApplicationServices()
        {
            services.AddSingleton<ISigningService, SigningService>();

            return services;
        }

        public IServiceCollection AddValidationApplicationServices()
        {
            // Use camel case for FluentValidation property names
            ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member?.Name.Humanize();
            ValidatorOptions.Global.PropertyNameResolver = (_, memberInfo, expression) =>
            {
                if (expression != null)
                {
                    var chain = PropertyChain.FromExpression(expression);
                    if (chain.Count > 0)
                    {
                        var propertyNames = chain.ToString().Split(ValidatorOptions.Global.PropertyChainSeparator);
                        if (propertyNames.Length == 1)
                        {
                            return propertyNames[0].Camelize();
                        }

                        return string.Join(
                            ValidatorOptions.Global.PropertyChainSeparator,
                            propertyNames.Select(n => n.Camelize()));
                    }
                }

                return memberInfo?.Name.Camelize();
            };

            return services;
        }
    }
}