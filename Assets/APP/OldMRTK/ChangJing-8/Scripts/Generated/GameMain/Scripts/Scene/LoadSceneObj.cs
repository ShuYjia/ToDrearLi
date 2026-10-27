/*
* @Description: 
* @Author: Li Yongqi
* @Date: 2024-06-14
* @LastEditTime: 2024-06-14
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 任务状态枚举
/// </summary>
namespace ChangJing_8.ExportRuntime
{
    public class LoadSceneObj : MonoBehaviour
    {
        //任务完成提示音
        public AudioSource audioSource;
        public bool _ismult = false;
        public static bool isMult = false;

        public enum Tesks
        {
            None,


            UIIntroduction,
            ServerTest,
            Checklocation,
            ServerPowerSupply,
            CabinetPowerSupply,
            ServerNetworking,
            ServerStartup,

            TakeU,
            InsertUSB,
            PressF11,
            ChangeLanguage,
            Entertime,
            Enterkeyboard,
            Enterinstall,
            Enterselect,
            SelectGNOME,
            EnterinstallUI,
            Startinstall,
            EnterROOT,
            Custompassword,
            Restartsystem,
            Enterlicence,
            Selectlicence,
            Complete,
            GoLanguage,
            Goinput,
            Goprivacy,
            Gotime,
            Goonline,
            GoMine,
            Gopassword,
            SelectStart,
            CloseUI,
            ClickBtn,
            Clickwired,
            ClickSet,
            Openwired,
            ClickSeton,
            ClickIPV4,
            Clickhand,
            InputAdd,
            IPconfiguration,

            RMSIntro,
            MobaXtermIntro,
            LinuxIntro,
            RMSTest,
            StartMobaXterm,
            InputIP,
            SSHLink,
            ImportPackage,
            Command1,
            Command2,
            Command3,
            Command4,
            Command5,
            Command6,
            Command7,
            Command8,
            Command9,
            Command10,
            Command11,
            Command12,
            Command13,
            DetectInstallation,

            UIME,
            TestME,



            UIintroduction,
            CodingTest,
            InkFountain,
            LaserLevel,
            QRCodePasting,

            UIHJ,
            TestHJ,
            InspectHJ,
            Placebase,
            Tiltingbase,
            Installupright,
            Putscrew,
            Dropglue,
            Setscrew,
            Downlaminate,
            Installbeam1,
            Hammerboard1,
            Putlaminate1,
            Installbeam2,
            Hammerboard2,
            Putlaminate2,
            Installbeam3,
            Hammerboard3,
            Putlaminate3,
            Putbox,
            InstallhJ,

            UIBZQ,
            TestBZQ,
            InspectBZQ,
            Setupright,
            Installbeam,
            Installbeam4,
            Installlaminate1,
            Installbeam5,
            Installlaminate2,
            Installbeam6,
            Installlaminate3,
            Installbeam7,
            Installlaminate4,
            Putgun,
            PutComputer,
            Putsupport,
            Putscrew1,
            PutKeyboard,
            Putdisplay,
            Installslot,
            Slotscrew,
            Terminal,
            Installtag,
            InstallBZQ,

            UICDZ,
            TestCDZ,
            Removetacks,
            Removelid,
            PasteQRcode,
            Installchargingpile,
            Openfrontcover,
            Removeantenna,
            Mountingantenna,
            Drillholes,
            Removetube,
            Putintohole,
            Nailing,
            Removescrew,
            Wrenchin,
            Pluginpower,
            AGVnetwork,
            OperatingRCS,

            UIGQR,
            TestGQR,
            Attachthedrill,
            RemoveTacks,
            RemoveLid,
            PushoutGQR,
            Checkfunctionality,
            Pressswitch,
            AGVnetWork,
            Sendrobot,

            Shelfgeneration,
            manipulationrobot,
            Handlingrack,
            Completeentry,
            CompletetESK

        }

        public static double score;

        public static Tesks task = Tesks.None;
        public Tesks Task
        {
            get
            {
                return task;
            }
            set
            {
                if (task == value + 1 )
                {
                    task = value;
                    audioSource.PlayOneShot(audioSource.clip);
                }
            }
        }
        public Tesks tesks = Tesks.None;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            // isMult = _ismult;
            //task = tesks;
            Debug.Log("当前任务：" + LoadSceneObj.task);

        }
    }
}
