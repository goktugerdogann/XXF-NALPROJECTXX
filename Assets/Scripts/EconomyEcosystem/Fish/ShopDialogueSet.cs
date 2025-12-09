using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopDialogueSet", menuName = "Game/Shop Dialogue Set")]
public class ShopDialogueSet : ScriptableObject
{
    [TextArea]
    public List<string> greetLines;        // "Welcome..."
    [TextArea]
    public List<string> offerOkLines;      // "Deal."
    [TextArea]
    public List<string> offerLowLines;     // "Too low."
    [TextArea]
    public List<string> counterOfferLines; // "Let's do this price instead."
    [TextArea]
    public List<string> angryLines;        // "You are wasting my time."
    [TextArea]
    public List<string> banLines;          // "Get out of my shop."
}
