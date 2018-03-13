﻿#region License
/*The MIT License (MIT)

One Window

Tracking_Loader - UI loader script

Copyright (C) 2018 DMagic
 
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using System.Collections;
using BetterTracking.Unity;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KSP.UI.Screens;

namespace BetterTracking
{
    [KSPAddon(KSPAddon.Startup.TrackingStation, true)]
    public class Tracking_Loader : MonoBehaviour
    {
        private const string prefabAssetName = "better_tracking_prefabs.btk";

        private static bool loaded;
        private static bool UILoaded;
        
        private static GameObject[] loadedPrefabs;

        private static GameObject _groupPrefab;
        private static GameObject _sortHeaderPrefab;

        private Sprite _backgroundSprite;
        private Sprite _checkmarkSprite;
        private Sprite _hoverSprite;
        private Sprite _activeSprite;
        private Sprite _normalSprite;
        private Sprite _inactiveSprite;

        private static VesselIconSprite _iconPrefab;

        public static VesselIconSprite IconPrefab
        {
            get { return _iconPrefab; }
        }

        public static GameObject GroupPrefab
        {
            get { return _groupPrefab; }
        }

        public static GameObject SortHeaderPrefab
        {
            get { return _sortHeaderPrefab; }
        }

        private void Awake()
        {
            if (loaded)
            {
                Destroy(gameObject);
                return;
            }

            if (loadedPrefabs == null)
            {
                string path = KSPUtil.ApplicationRootPath + "GameData/TrackingStationEvolved/Resources/";

                AssetBundle prefabs = AssetBundle.LoadFromFile(path + prefabAssetName);

                if (prefabs != null)
                    loadedPrefabs = prefabs.LoadAllAssets<GameObject>();
            }

            StartCoroutine(WaitForTrackingList());
        }

        private IEnumerator WaitForTrackingList()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);

            SpaceTracking _TrackingStation = null;

            while (_TrackingStation == null)
            {
                _TrackingStation = GameObject.FindObjectOfType<SpaceTracking>();

                if (_TrackingStation == null)
                    yield return wait;
            }

            processSprites(_TrackingStation);

            if (loadedPrefabs != null)
                processUIPrefabs();

            if (UILoaded)
                loaded = true;

            Tracking_Utils.TrackingLog("UI Loaded");

            Destroy(gameObject);
        }

        private void processSprites(SpaceTracking tracking)
        {
            var prefab = tracking.listItemPrefab;

            if (prefab == null)
                return;

            prefab.gameObject.AddOrGetComponent<Tracking_WidgetListener>();

            _iconPrefab = prefab.iconSprite;

            Selectable toggle = prefab.toggle.GetComponent<Selectable>();

            if (toggle == null)
                return;

            _normalSprite = toggle.image.sprite;
            _hoverSprite = toggle.spriteState.highlightedSprite;
            _activeSprite = toggle.spriteState.pressedSprite;
            _inactiveSprite = toggle.spriteState.disabledSprite;

            var images = prefab.GetComponentsInChildren<Image>();

            if (images == null || images.Length < 2)
                return;

            _backgroundSprite = images[images.Length - 2].sprite;

            _checkmarkSprite = ((Image)prefab.toggle.graphic).sprite;
        }

        private void processUIPrefabs()
        {
            for (int i = loadedPrefabs.Length - 1; i >= 0; i--)
            {
                GameObject o = loadedPrefabs[i];

                if (o == null)
                    continue;

                //Tracking_Utils.TrackingLog(o.name);

                if (o.name == "HeaderGroup")
                    _groupPrefab = o;
                else if (o.name == "SortHeader")
                    _sortHeaderPrefab = o;

                processTMP(o);
                processUIComponents(o);
            }

            UILoaded = true;
        }
        
        private void processTMP(GameObject obj)
        {
            TextHandler[] handlers = obj.GetComponentsInChildren<TextHandler>(true);

            if (handlers == null)
                return;

            for (int i = 0; i < handlers.Length; i++)
                TMProFromText(handlers[i]);
        }

        private void TMProFromText(TextHandler handler)
        {
            if (handler == null)
                return;

            Text text = handler.GetComponent<Text>();

            if (text == null)
                return;

            string t = text.text;
            Color c = text.color;
            int i = text.fontSize;
            bool r = text.raycastTarget;
            FontStyles sty = TMPProUtil.FontStyle(text.fontStyle);
            TextAlignmentOptions align = TMPProUtil.TextAlignment(text.alignment);
            float spacing = text.lineSpacing;
            GameObject obj = text.gameObject;

            MonoBehaviour.DestroyImmediate(text);

            Tracking_TMP tmp = obj.AddComponent<Tracking_TMP>();

            tmp.text = t;
            tmp.color = c;
            tmp.fontSize = i;
            tmp.raycastTarget = r;
            tmp.alignment = align;
            tmp.fontStyle = sty;
            tmp.lineSpacing = spacing;

            tmp.font = UISkinManager.TMPFont;
            tmp.fontSharedMaterial = Resources.Load("Fonts/Materials/Calibri Dropshadow", typeof(Material)) as Material;
            
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.isOverlay = false;
            tmp.richText = true;
        }
        
        private void processUIComponents(GameObject obj)
        {
            TrackingStyle[] styles = obj.GetComponentsInChildren<TrackingStyle>(true);

            if (styles == null)
                return;

            for (int i = 0; i < styles.Length; i++)
                processComponents(styles[i]);
        }

        private void processComponents(TrackingStyle style)
        {
            if (style == null)
                return;

            UISkinDef skin = UISkinManager.defaultSkin;

            if (skin == null)
                return;

            switch (style.StlyeType)
            {
                case TrackingStyle.StyleTypes.Toggle:
                    style.setToggle(_normalSprite, _hoverSprite, _activeSprite, _inactiveSprite, _checkmarkSprite);
                    break;
                case TrackingStyle.StyleTypes.IconBackground:
                    style.setImage(_backgroundSprite);
                    break;
                case TrackingStyle.StyleTypes.Button:
                    style.setButton(skin.button.normal.background, skin.button.highlight.background, skin.button.active.background, skin.button.disabled.background);
                        break;
            }
        }
    }
}