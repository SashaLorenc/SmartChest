namespace SmartChest.UI
{
    /// <summary>Une seule fenêtre du mod ouverte à la fois : chaque fenêtre ferme les autres en s'ouvrant.</summary>
    internal static class Panels
    {
        internal static void CloseAll()
        {
            ChestPanel.Close();
            ItemEditorPanel.Close();
            CategoryManagerPanel.Close();
            SearchPanel.Close();
        }
    }
}
