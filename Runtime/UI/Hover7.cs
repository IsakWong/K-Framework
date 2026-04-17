using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.EventSystems;


public class Hover7 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public MMF_Player Player;

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

        transform.DOScale(Vector3.one * 1.1f, 0.5f).SetEase(Ease.InBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {

        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }
}