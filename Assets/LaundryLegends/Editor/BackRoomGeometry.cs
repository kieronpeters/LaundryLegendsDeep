namespace LaundryLegends.Editor
{
    /// <summary>
    /// One source of truth for the back-room doorway cut and hinge, shared by the
    /// room builder, the playtest repair tool and the door controller wiring.
    /// </summary>
    public static class BackRoomGeometry
    {
        public const float DoorWidth = 1.6f;
        public const float DoorHalfWidth = DoorWidth * 0.5f;
        public const float DoorHingeLocalZ = 0.6f;

        public static float GapMin(float doorZ)
        {
            return doorZ - DoorHalfWidth;
        }

        public static float GapMax(float doorZ)
        {
            return doorZ + DoorHalfWidth;
        }
    }
}
