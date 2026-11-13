namespace Melodia.Services;

public static class EqualizerPresets
{
    /// <summary>The 10 bands LibVLC's equalizer exposes.</summary>
    public static readonly string[] BandLabels =
        { "60 Hz", "170 Hz", "310 Hz", "600 Hz", "1 kHz", "3 kHz", "6 kHz", "12 kHz", "14 kHz", "16 kHz" };

    public static readonly IReadOnlyList<(string Name, double[] Gains)> All = new (string, double[])[]
    {
        ("Flat",         new double[] {  0,  0,  0,  0,  0,  0,  0,  0,  0,  0 }),
        ("Bass Boost",   new double[] {  6,  5,  4,  2,  0,  0,  0,  0,  0,  0 }),
        ("Treble Boost", new double[] {  0,  0,  0,  0,  0,  1,  3,  5,  6,  6 }),
        ("Rock",         new double[] {  5,  4,  3,  1, -1, -1,  2,  3,  4,  5 }),
        ("Pop",          new double[] { -1,  2,  4,  5,  3,  0, -1, -1, -1, -1 }),
        ("Hip-Hop",      new double[] {  5,  4,  2,  3, -1, -1,  2, -1,  2,  3 }),
        ("Electronic",   new double[] {  5,  4,  1, -1, -2,  1,  0,  2,  4,  5 }),
        ("Dance",        new double[] {  6,  5,  3,  0,  0, -2, -3, -3,  0,  0 }),
        ("Jazz",         new double[] {  3,  2,  1,  2, -2, -2,  0,  1,  2,  3 }),
        ("Classical",    new double[] {  3,  2,  1,  0,  0,  0, -1, -1, -2, -2 }),
        ("Vocal",        new double[] { -3, -2, -1,  2,  4,  4,  3,  1,  0, -1 }),
        ("Acoustic",     new double[] {  4,  4,  3,  1,  2,  2,  3,  3,  3,  2 }),
    };
}
