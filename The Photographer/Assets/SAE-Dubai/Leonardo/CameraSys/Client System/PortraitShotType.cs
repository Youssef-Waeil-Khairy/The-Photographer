namespace SAE_Dubai.Leonardo.CameraSys.Client_System
{
    /// <summary>
    /// Defines different types of portrait composition based on framing distance.
    /// </summary>
    public enum PortraitShotType
    {
        Undefined = -1, // Default/Error state.
        ExtremeCloseUp, // Eyes, mouth details only (cutting the face).
        CloseUp, // Face fills the frame.
        MediumCloseUp, // Head and shoulders.
        Medium, // Waist up.
        MediumWide, // AKA american shot (from up the knees).
        Wide, // Subject occupies less than half the frame (full body).
        ExtremeWide // Subject is small in frame, environment dominates.
    }
}