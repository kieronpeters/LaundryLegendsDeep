using LaundryLegends.Progression;
using UnityEngine;

namespace LaundryLegends.UI
{
    /// <summary>
    /// The shop map, opened with M. It shows the laundromat, and once the back room
    /// has been opened, that room and the arcade machine inside it. Before the
    /// reveal it explains that no plan exists yet.
    /// </summary>
    public sealed class ShopMapView : MonoBehaviour
    {
        [SerializeField] private SecretRoomController _room;
        [SerializeField] private KeyCode _toggleKey = KeyCode.M;

        public bool IsOpen { get; private set; }

        public bool HasBackRoom => _room != null && _room.IsRevealed;

        public void Configure(SecretRoomController room)
        {
            _room = room;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                IsOpen = !IsOpen;
            }
        }

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            const float width = 520f;
            const float height = 300f;
            var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUI.Box(panel, "SHOP MAP");
            GUI.Label(new Rect(panel.x + 16f, panel.y + 26f, width - 32f, 20f),
                HasBackRoom ? "Back room unlocked - arcade marked" : "No plan of the back room yet");
            GUI.Label(new Rect(panel.x + 16f, panel.y + height - 26f, width - 32f, 20f), "M closes the map");

            // Laundromat footprint.
            var shop = new Rect(panel.x + 30f, panel.y + 60f, 300f, 190f);
            GUI.Box(shop, "LAUNDROMAT");

            if (!HasBackRoom)
            {
                return;
            }

            // The back room is roughly half the shop, through the east door.
            var backRoom = new Rect(shop.xMax + 8f, panel.y + 90f, 150f, 130f);
            GUI.Box(backRoom, "BACK ROOM");

            var arcade = new Rect(backRoom.x + 20f, backRoom.y + 40f, 46f, 26f);
            GUI.Box(arcade, "ARCADE");

            Vector2 arrow = new Vector2(shop.xMax + 2f, panel.y + 150f);
            GUI.Label(new Rect(arrow.x, arrow.y, 24f, 20f), ">");
        }
    }
}
