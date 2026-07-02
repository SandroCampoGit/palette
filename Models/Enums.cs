namespace PulseArtists.Models;

public enum PrimaryMode
{
    Artist = 0,   // "Creator" – wants to be found
    Finder = 1    // wants to find artists to collab with
}

public enum Discipline
{
    Musician = 0,
    DJ = 1,
    Producer = 2,
    Vocalist = 3,
    Photographer = 4,
    Videographer = 5,
    Painter = 6,
    Illustrator = 7,
    GraphicDesigner = 8,
    Dancer = 9,
    Actor = 10,
    Writer = 11,
    TattooArtist = 12,
    Other = 13
}

public enum CollabStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2
}
