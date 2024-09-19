using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MousePalette : MonoBehaviour
{

    private Material material;
    [SerializeField] int currPalette = 0;
    private static Dictionary<string, string[]> palettes = new Dictionary<string, string[]>{
        //fur lightest, fur light, fur dark, outline, NET light, NET dark, eyes
        {"vanilla", new string[] {"#FFFFFF","#E1EAED","#9BADB7","#000000","#D77BBA","#DA5763","#1D1B1B"}},
        {"suntan", new string[] {"#EEC39A","#D9A066","#8F563B","#000000","#D95763","#AC3232","#323C39"}},
        {"græy", new string[] {"#847E87","#696A6A","#595652","#000000","#D6788C","#CB5971","#222034"}},
        {"nightcrawler", new string[] {"#575252","#464141","#292626","#000000","#7a4242","#572020","#b9b558"}},
        {"cinnamon", new string[] {"#663931","#512d26","#341b17","#000000","#a8635b","#8b453d","#a9d65a"}},
        {"cookie dough", new string[] {"#e8d5c3","#cbb199","#7a6653","#000000","#d6a9c1","#b97f9e","#3f3f74"}},
    };

    public void Prev() {
        if (currPalette == 0) {
            currPalette = palettes.Count - 1;
        } else {
            currPalette--;
        }
        SwapPalette(currPalette);
    }

    public void Next() {
        if (currPalette == palettes.Count - 1) {
            currPalette = 0;
        } else {
            currPalette++;
        }
        SwapPalette(currPalette);
    }

    // Start is called before the first frame update
    void Start()
    {
        material = GetComponent<Renderer>().material;
        SwapPalette(currPalette);
    }

    private Color c;
    void SwapPalette(int p) {
        var palette = palettes.ElementAt(p).Value;
        ColorUtility.TryParseHtmlString(palette[0], out c);
        material.SetColor("_furColorLightest", c);
        ColorUtility.TryParseHtmlString(palette[1], out c);
        material.SetColor("_furColorLight", c);
        ColorUtility.TryParseHtmlString(palette[2], out c);
        material.SetColor("_furColorDark", c);
        ColorUtility.TryParseHtmlString(palette[3], out c);
        material.SetColor("_outlineColor", c);
        ColorUtility.TryParseHtmlString(palette[4], out c);
        material.SetColor("_noseEarsTailLight", c);
        ColorUtility.TryParseHtmlString(palette[5], out c);
        material.SetColor("_noseEarsTailDark", c);
        ColorUtility.TryParseHtmlString(palette[6], out c);
        material.SetColor("_eyeColor", c);
    }

    // Update is called once per frame
    void Update()
    {
        SwapPalette(currPalette);
    }
}
