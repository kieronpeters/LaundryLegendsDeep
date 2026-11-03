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

            const float width = 560f;
            const float height = 340f;
            var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            Color previous = GUI.color;
            GUI.color = new Color(0.03f, 0.04f, 0.06f, 0.95f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;

            GUI.Box(panel, "SHOP MAP");
            GUIStyle body = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            GUI.Label(new Rect(panel.x + 18f, panel.y + 28f, width - 36f, 22f),
                HasBackRoom
                    ? "Back room unlocked - arcade marked"
                    : "Back room not discovered yet. Explore the shop and follow the clues.",
                body);

            // Laundromat footprint with the main fixtures.
            var shop = new Rect(panel.x + 26f, panel.y + 64f, 330f, 210f);
            GUI.Box(shop, "LAUNDROMAT");
            GUI.Box(new Rect(shop.x + 20f, shop.y + 34f, 84f, 26f), "Counter");
            GUI.Box(new Rect(shop.x + 20f, shop.y + 120f, 150f, 26f), "Machines");
            GUI.Box(new Rect(shop.x + 200f, shop.y + 150f, 96f, 26f), "Waiting");
            GUI.Box(new Rect(shop.x + 120f, shop.y + 70f, 70f, 26f), "Fold");

            if (!HasBackRoom)
            {
                GUI.Box(new Rect(shop.xMax + 18f, panel.y + 110f, 130f, 90f), "?");
                GUI.Label(new Rect(shop.xMax + 26f, panel.y + 150f, 116f, 40f), "Uncharted", body);
                GUI.Label(new Rect(panel.x + 18f, panel.y + height - 28f, width - 36f, 20f),
                    "M closes the map", body);
                return;
            }

            // The back room is roughly half the shop, through the east door.
            var backRoom = new Rect(shop.xMax + 18f, panel.y + 90f, 160f, 140f);
            GUI.Box(backRoom, "BACK ROOM");

            var arcade = new Rect(backRoom.x + 22f, backRoom.y + 44f, 58f, 28f);
            GUI.Box(arcade, "ARCADE");

            Vector2 arrow = new Vector2(shop.xMax + 4f, panel.y + 158f);
            GUI.Label(new Rect(arrow.x, arrow.y, 24f, 20f), ">");
            GUI.Label(new Rect(panel.x + 18f, panel.y + height - 28f, width - 36f, 20f),
                "M closes the map", body);
        }
    }
}
