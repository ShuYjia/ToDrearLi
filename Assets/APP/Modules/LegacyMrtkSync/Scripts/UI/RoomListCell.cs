using UnityEngine;
using TMPro;
using UnityEngine.UI;
namespace _syc_resource._my_scripts
{
    public class RoomListCell : MonoBehaviour
    {
        public TMP_Text roomIdText;
        public TMP_Text roomNameText;
        public TMP_Text hostNameText;
        public TMP_Text playersText;
        public GameObject lockIcon; // 有密码时显示的图标
        public Button joinButton;

        private string _ipAddress;
        private ushort _port;
        private bool _hasPassword;
        private RoomBrowserCtrl _browserCtrl;

        public void InitRoomData(string ip, DiscoveryResponseData data, RoomBrowserCtrl browserCtrl)
        {
            _ipAddress = ip;
            _port = data.Port;
            _hasPassword = data.HasPassword;
            _browserCtrl = browserCtrl;

            // ToString() 是因为我们在数据结构里用了 FixedString64Bytes
            roomIdText.text = data.RoomId.ToString();
            roomNameText.text = data.ServerName.ToString();
            hostNameText.text = data.HostName.ToString();
            playersText.text = $"{data.CurrentPlayers} / {data.MaxPlayers}";
        
            if (lockIcon != null) lockIcon.SetActive(_hasPassword);

            // 如果房间满了，禁用加入按钮
            joinButton.interactable = data.CurrentPlayers < data.MaxPlayers;

            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinClicked);
        }

        private void OnJoinClicked()
        {
            _browserCtrl.QuickJoin();
        }
    }
}