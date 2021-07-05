using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayMusicalPatterns : MonoBehaviour
{
    SimHyperParameters Sim;
    //Canvases containing the score assets
    public Canvas twoEighths;
    public Canvas fourEighths;
    public Canvas bar;
    public Canvas dottedHalf;
    public Canvas eighth;
    public Canvas eighthRest;
    public Canvas half;
    public Canvas halfRest;
    public Canvas quarter;
    public Canvas quarterRest;
    public Canvas whole;
    public Canvas wholeRest;
    public Canvas cursor;
    public Canvas text;
    private float scale;


    //Width of a reference musical note in pixels
    private int pixelWidth=100;
    //Height of a reference musical note in pixels
    private int pixelHeight=200;
    // Start is called before the first frame update
    void Awake()
    {
        Sim = FindObjectOfType<SimHyperParameters>();
        Debug.Log(Sim.getMusicalPatternLength());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void showMusicalPatterns()
    {
        //define scale so that the musical patterns take 1/3rd of the screen
        scale = Screen.width / (3.0f * (Sim.getMusicalPatternLength() * pixelWidth));
        //shows a maximum of 5 patterns
        int numberOfPatternsShown = Mathf.Min(4, Sim.getRankedMP().Count);
        for (int i = 0;i<numberOfPatternsShown;i++)
        {
            printMPToPlace(Sim.getRankedMP()[i].musicalPattern,-(int)Mathf.Floor(Sim.getMusicalPatternLength() * pixelWidth * scale),(int)Mathf.Floor(((float)(numberOfPatternsShown-i)-1.0f/2.0f)*pixelHeight*scale),scale,Sim.getRankedMP()[i].agentNumber);
        }
    }

    public void printMPToPlace(int[] MP,int x, int y, float scale,int agentNumber)
    {
        Canvas numberCanvas = Instantiate(text);
        numberCanvas.gameObject.transform.Find("Text").GetComponent<Text>().text =agentNumber.ToString();
        numberCanvas.gameObject.transform.Find("Text").GetComponent<RectTransform>().anchoredPosition = new Vector2(x - 5, y);
        int numberOfBars = (int)Mathf.Ceil((float)(Sim.getMusicalPatternLength()) / 8.0f);
        for (int j = 0; j < numberOfBars; j++)
        {
            Canvas b = Instantiate(bar);
            b.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
            //A translation of 3.5f*pixelWidth*scale is applied because a bar is 8 times wider than a usual note
            b.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector2(x + scale * 8 * pixelWidth * j + 3.5f*pixelWidth*scale, y);
            //A cursor is instantiated at the beginning of the bar
            Canvas c = Instantiate(cursor);
            c.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
            c.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector2(x , y);
        }
        int i = 0;
        while(i<MP.Length)
        {
            if(MP[i]==1)
            {
                if(i+1>=MP.Length)
                {
                    Canvas n = Instantiate(eighth);
                    n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                    n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale,y);
                    i += 1;
                }
                else if(MP[i+1]==1)
                {
                    if(i+3<MP.Length && MP[i+2]==1 && MP[i+3]==1)
                    {
                        Canvas n = Instantiate(fourEighths);
                        n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                        //A translation of 1.5f*pixelWidth * scale is applied because this note is 4 times wider than the others
                        n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale + 1.5f*pixelWidth * scale,y,0);
                        i += 4;
                    }
                    else
                    {
                       Canvas n = Instantiate(twoEighths);
                        n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                        //A translation of 0.5f*pixelWidth * scale is applied because this note is twice as wide as the others
                        n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale + 0.5f*pixelWidth * scale,y,0);
                        i += 2; 
                    }
                }
                else
                {
                    if(i+3<MP.Length && MP[i+2]==0 && MP[i+3]==0)
                    {
                        if(i+5<MP.Length && MP[i+4]==0 && MP[i+5]==0)
                        {
                            if(i+7<MP.Length && MP[i+6]==0 && MP[i+7]==0)
                            {
                                Canvas n = Instantiate(whole);
                                n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                                n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale,y,0);
                                i += 8; 
                            }
                            else
                            {
                                Canvas n = Instantiate(dottedHalf);
                                n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                                n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale,y,0);
                                i += 6; 
                            }
                        }
                        else
                        {
                            Canvas n = Instantiate(half);
                            n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                            n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale,y,0);
                            i += 4; 
                        }
                    }
                    else
                    {
                       Canvas n = Instantiate(quarter);
                        n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                        n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale,y,0);
                        i += 2;  
                    }
                }
            }
            else if(MP[i]==0)
            {
                if(i+1<MP.Length && MP[i+1]==0)
                {
                    if(i+3<MP.Length && MP[i+2]==0 && MP[i+3]==0)
                    {
                        if(i+7<MP.Length && MP[i+4]==0 && MP[i+5]==0 && MP[i+6]==0 && MP[i+7]==0)
                        {
                            Canvas n = Instantiate(wholeRest);
                            n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                            n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale,y,0);
                            i += 8; 
                        }
                        else
                        {
                            Canvas n = Instantiate(halfRest);
                            n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                            n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale,y,0);
                            i += 4; 
                        }
                    }
                    else
                    {
                        Canvas n = Instantiate(quarterRest);
                        n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                        n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale,y,0);
                        i += 2; 
                    }
                }
                else
                {
                    Canvas n = Instantiate(eighthRest);
                    n.gameObject.transform.Find("Image").GetComponent<RectTransform>().transform.localScale = new Vector3(scale, scale, scale);
                    n.gameObject.transform.Find("Image").GetComponent<RectTransform>().anchoredPosition = new Vector3(x + i * pixelWidth * scale,y,0);
                    i += 1; 
                }
            }
        }
    }

    public void translateCursors()
    {
        GameObject[] cursors = GameObject.FindGameObjectsWithTag("Cursor");
        for (int i = 0; i < cursors.Length;i++)
        {
            cursors[i].transform.Find("Image").GetComponent<RectTransform>().anchoredPosition +=pixelWidth*scale*Vector2.right;
        }
    }
}

