using System;
using UnityEngine;

// What the world map screen knows about every room, made by DungeonBuilder from the map files (the
// maps themselves aren't in the game): where each room sits on the world map (the "world:" header),
// its tiles boiled down to a few kinds of ground, and the places worth marking: fountains, dragon eggs
// and gaps. WorldMapView draws the rooms you've visited from this.
//
// Tile kinds in Room.rows: ' ' nothing, '#' wall, 'H' hedge, '.' ground, '=' path, 'w' water, '~' lava, 'b' a building.
public class WorldMapData : ScriptableObject
{
    [Serializable]
    public class Marker
    {
        public string kind;   // "fountain", "egg", "gap", "exit" (a way into another room) or "lockedexit" (stairs that wait for the room to be cleared)
        public int col, row;
        public string id;     // egg: the item id; gap: "Scene/col,row" (what HintBubble remembers having seen); exit: the room it leads to
    }

    [Serializable]
    public class Room
    {
        public string scene, title;
        public int x, y;                 // where its top-left corner sits on the world map, in tiles
        public int width, height;
        public string[] rows;
        public Color floorColor, wallColor;
        public Marker[] markers;
        public string[] secretTiles;     // "col,row": drawn as nothing until the level's secret is found
    }

    public Room[] rooms = new Room[0];

    public Room Find(string scene)
    {
        foreach (var room in rooms) if (room.scene == scene) return room;
        return null;
    }
}
