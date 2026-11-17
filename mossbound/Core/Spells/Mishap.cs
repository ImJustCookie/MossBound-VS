namespace Mossbound.Core;

// What a broken circle does instead of its spell.
public enum Mishap
{
    Overload,   // more than ElementMixer.MaxIntensity of one element in a heart
    Opposition, // opposite elements in the same heart
    Misplaced,  // an element written outside the heart
    NoElement,  // nothing to cast
}
