using Ezg.Core.Extensions;
using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.Systems;

public class SpecialOfferController : MonoBehaviour
{
    public GameObject[] AllPack;

    // Start is called before the first frame update
    private void Start()
    {
        EventManager.StartListening(EventName.UpdateResource, UpdateView);
        EventManager.StartListening(EventName.PurchasedIapSuccess, () => this.DelayMethod(0.1f, UpdateView));
        this.DelayMethod(0.05f, UpdateView);
    }

    // Update is called once per frame
    private void UpdateView()
    {
        var isShow = false;
        foreach (var a in AllPack)
            if (a.gameObject.activeSelf)
            {
                isShow = true;
                break;
            }

        gameObject.SetActive(isShow);
    }
}