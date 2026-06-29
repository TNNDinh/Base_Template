public class IconViewPack : IconView
{
    public override void SetData(Resource resource, int rarity = -1, bool isHavePlus = false)
    {
        base.SetData(resource, rarity);
        //value.text += " " + PlayerResource.GetResName(resource);
    }
}