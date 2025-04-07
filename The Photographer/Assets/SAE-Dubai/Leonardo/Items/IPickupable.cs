namespace SAE_Dubai.Leonardo.Items
{
    public interface IPickupable
    {
        public string GetItemName();

        public void OnPickup();
        
        public void OnDrop();
    }
}