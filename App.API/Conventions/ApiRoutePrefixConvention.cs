using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace App.API.Conventions;

public sealed class ApiRoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel _routePrefix;

    public ApiRoutePrefixConvention(IRouteTemplateProvider routeTemplateProvider)
    {
        _routePrefix = new AttributeRouteModel(routeTemplateProvider);
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel is null
                    ? _routePrefix
                    : AttributeRouteModel.CombineAttributeRouteModel(_routePrefix, selector.AttributeRouteModel);
            }
        }
    }
}
