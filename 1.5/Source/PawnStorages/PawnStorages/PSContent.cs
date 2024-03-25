using UnityEngine;
using Verse;

namespace PawnStorages;

[StaticConstructorOnStartup]
public static class PSContent
{
    public static Texture2D DirectionArrow = ContentFinder<Texture2D>.Get("UI/Direction");
}
