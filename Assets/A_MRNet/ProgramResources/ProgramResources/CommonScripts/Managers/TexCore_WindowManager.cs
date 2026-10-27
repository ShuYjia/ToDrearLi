using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

	public class Window
	{
	    public GameObject win;
	    public Action action;
	}
	
	public class TexCore_WindowManager : MonoBehaviour
	{
	    public static TexCore_WindowManager self;
	
	    public GameObject pre_singleHint;//提示框
	    public GameObject pre_promis;//确认框

		public bool isPC;
	    public List<Window> allWindows ;
	
	    private void Awake()
	    {
			if (TexCore_Const.partfrom == Partfrom.PC && isPC)
			{
				if (self == null)
				{
					self = this;
				}
				else
				{
					this.enabled = false;
				}
			}
			else if (TexCore_Const.partfrom == Partfrom.VR && !isPC)
			{
				if (self == null)
				{
					self = this;
				}
				else
				{
					this.enabled = false;
				}
			}
			
			allWindows = new List<Window>();
	    }
	
	    public void OnWindowClick(GameObject obj)
	    {
	        if (allWindows!=null&&allWindows.Count>0)
	        {
	            switch (obj.name)
	            {
	                case "w_确认":
	                    if (allWindows[allWindows.Count - 1].action!=null)
	                    {
	                        allWindows[allWindows.Count - 1].action();
	                    }
	                    Destroy( allWindows[allWindows.Count - 1].win);
	                    allWindows.RemoveAt(allWindows.Count - 1);
	                    break;
	                case "w_取消":
	                    Destroy(allWindows[allWindows.Count - 1].win);
	                    allWindows.RemoveAt(allWindows.Count - 1);
	                    break;
	                default:
	                    break;
	            }
	        }
	    }
	
	    public void CreateHit(string hit)
	    {
	        Window newWin = new Window();
	        newWin.win = Instantiate(pre_singleHint, transform);
	        newWin.win.SetActive(true);
	        newWin.win.transform.Find("hint") .GetComponent< TextMeshProUGUI>().text = hit.Replace("\\n", "\n");
	        allWindows.Add(newWin);
	    }
	
	    public void CreatePromis(string hit,Action action=null)
	    {
	        Window newWin = new Window();
	        newWin.win = Instantiate(pre_promis, transform);
	        newWin.win.SetActive(true);
	        newWin.win.transform.Find("hint").GetComponent<TextMeshProUGUI>().text=hit.Replace("\\n", "\n");
	        newWin.action = action;
	        allWindows.Add(newWin);
	    }
	
}
