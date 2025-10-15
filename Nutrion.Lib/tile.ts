/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

import { Player } from "./player";
import { TileContent } from "./tile-content";

export class Tile {
    id: number;
    q: number;
    r: number;
    ownerId: string = "";
    playerId: string;
    player: Player;
    color: string = "";
    lastUpdated: Date = new Date("2025-10-15T20:11:54.9331185+00:00");
    contents: TileContent[] = [];
}
