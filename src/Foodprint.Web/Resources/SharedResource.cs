namespace Foodprint.Web.Resources;

/// <summary>
/// Marker type for the app-wide string catalogue. Inject
/// <c>IStringLocalizer&lt;SharedResource&gt;</c> and resolve keys against
/// <c>Resources/SharedResource.{culture}.resx</c> (the neutral file holds Spanish).
/// </summary>
public sealed class SharedResource;
