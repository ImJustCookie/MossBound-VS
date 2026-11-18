using System;

namespace Mossbound.Core;

// What hearts and form groups can become: every element and form alone is itself; groups need a mix below.
public static class Recipes
{
    public static readonly Mixer<Element, Aspect> Hearts = new(
        element => Enum.Parse<Aspect>(element.ToString()),
        new[]
        {
            (new[] { Element.Water, Element.Ember }, Aspect.Steam),
            (new[] { Element.Water, Element.Life }, Aspect.Rain),
            (new[] { Element.Ember, Element.Wind }, Aspect.Smoke),
        },
        new[] { (Element.Ember, Element.Frost), (Element.Wind, Element.Ward) });

    public static readonly Mixer<Form, Form> Forms = new(
        form => form,
        Array.Empty<(Form[], Form)>(),
        Array.Empty<(Form, Form)>());
}
