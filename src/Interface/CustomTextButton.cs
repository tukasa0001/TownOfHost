using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Button = UnityEngine.UI.Button;
using Object = UnityEngine.Object;

namespace TownOfHost.Interface;

public class CustomTextButton
{

    public ToggleButtonBehaviour Button;
    public PassiveButton PassiveButton;


    private Color toggleTextColor;
    private Color toggleBackgroundColor;
    private Color backgroundColor;
    private Color textColor;
    private Color hoverBackgroundColor;
    private Color hoverTextColor;

    public bool State { get; private set; }
    private bool toggle;

    public Transform Transform => Button.transform;
    public TextMeshPro Text => Button.Text;
    public SpriteRenderer Background => Button.Background;
    private List<Action> onClickActions = new();
    private List<Action> onMouseEnterActions = new();
    private List<Action> onMouseExitActions = new();

    private CustomTextButton(Transform parent)
    {
        ToggleButtonBehaviour sample = HudManager.Instance.GameMenu.ColorBlindButton;
        if (sample == null) throw new Exception("Failed to Grab ColorBlindButton");
        Button = Object.Instantiate(sample, parent);
        Button.BaseText = StringNames.Nevermind;
        Button.Text.enabled = false;
        Button.Background.enabled = false;
        Button.Background = Template.GetSpriteRenderer(Button.transform);
        Button.Text = Template.GetTextMeshPro(Button.transform);

        textColor = Button.Text.color;
        backgroundColor = Button.Background.color;
        hoverBackgroundColor = backgroundColor;
        hoverTextColor = textColor;
        toggleBackgroundColor = backgroundColor;
        toggleTextColor = textColor;


        Button.Background.transform.localScale = new Vector3(0.5f, 1);
        Button.Text.transform.localScale = new Vector3(0.5f, 1);
        //TODO: figure out math behind this
        Button.Text.transform.localPosition += new Vector3(1.31f, 0);
        PassiveButton = Button.GetComponent<PassiveButton>();
        PassiveButton.transform.localScale = new Vector3(2f, 1);

        PassiveButton.OnMouseOver.RemoveAllListeners();
        PassiveButton.OnMouseOut.RemoveAllListeners();
        PassiveButton.OnMouseOver.AddListener((UnityAction)OnMouseEnter);
        PassiveButton.OnMouseOut.AddListener((UnityAction)OnMouseExit);
        PassiveButton.OnClick = new Button.ButtonClickedEvent();
        PassiveButton.OnClick.AddListener((UnityAction)OnMouseClick);
    }

    public static CustomTextButton Create(Transform parent) => new(parent);

    public void SetScale(Vector3 scale)
    {
        this.PassiveButton.transform.localScale = scale;
        this.Text.transform.localScale = this.Background.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
    }

    private void OnMouseClick()
    {
        if (this.toggle)
        {
            this.State = !this.State;
            ShowStateColor();
        }
        this.onClickActions.Do(action => action.Invoke());
    }
    private void OnMouseEnter()
    {
        this.onMouseEnterActions.Do(action => action.Invoke());
        Button.Background.color = hoverBackgroundColor;
        Button.Text.color = hoverTextColor;
        ShowStateColor(false);
    }

    private void OnMouseExit()
    {
        this.onMouseExitActions.Do(action => action.Invoke());
        Button.Background.color = backgroundColor;
        Button.Text.color = textColor;
        ShowStateColor(false);
    }

    public void SetState(bool state)
    {
        this.State = state;
        ShowStateColor();
    }

    private void ShowStateColor(bool reset = true)
    {
        if (reset)
        {
            Background.color = backgroundColor;
            Text.color = textColor;
        }

        if (!this.State) return;
        Background.color = toggleBackgroundColor;
        Text.color = toggleTextColor;
    }


    public void AddOnClickHandle(Action action) => this.onClickActions.Add(action);
    public void AddMouseEnterHandle(Action action) => this.onMouseEnterActions.Add(action);
    public void AddMouseExitHandle(Action action) => this.onMouseExitActions.Add(action);
    public void SetHoverBackgroundColor(Color color) => this.hoverBackgroundColor = color;
    public void SetHoverTextColor(Color color) => this.hoverTextColor = color;
    public void SetToggleBackgroundColor(Color color) => this.toggleBackgroundColor = color;
    public void SetToggleTextColor(Color color) => this.toggleTextColor = color;
    public void AllowToggle(bool toggle) => this.toggle = toggle;
}