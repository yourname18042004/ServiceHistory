let socket;
let localIP = "Không xác định";

function connectWebSocket() {
  socket = new WebSocket('ws://localhost:9000');

  // Khi kết nối thành công
  socket.onopen = function(event) {
    console.log("Kết nối WebSocket đã mở thành công.");
  };

  // Khi nhận được tin nhắn từ server
  socket.onmessage = function(event) {
    console.log("Nhận được tin nhắn từ server: " + event.data);
  };

  // Khi có lỗi xảy ra
  socket.onerror = function(error) {
    console.error("Lỗi WebSocket:", error);
  };

  // Khi kết nối bị đóng
  socket.onclose = function(event) {
    //console.log("Kết nối WebSocket đã bị đóng, thử kết nối lại sau 3 giây...");
    //setTimeout(connectWebSocket, 3000); // Tự động kết nối lại sau 3 giây
  };
}

// Hàm lấy địa chỉ IP cục bộ
function getLocalIP() {
  const peerConnection = new RTCPeerConnection();
  peerConnection.createDataChannel('');
  peerConnection.createOffer()
    .then((offer) => peerConnection.setLocalDescription(offer))
    .catch((error) => console.error("Lỗi lấy IP cục bộ:", error));

  peerConnection.onicecandidate = (event) => {
    if (event.candidate && event.candidate.candidate) {
      const ipMatch = event.candidate.candidate.match(/\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b/);
      if (ipMatch) {
        localIP = ipMatch[0];
        console.log("Địa chỉ IP cục bộ là:", localIP);
        peerConnection.close();
      }
    }
  };
}

// Khởi tạo kết nối WebSocket và lấy IP
connectWebSocket();
getLocalIP();

// Khi có sự thay đổi tab, theo dõi URL
chrome.tabs.onUpdated.addListener(function(tabId, changeInfo, tab) {
  if (changeInfo.url) {
    console.log("Trang web mới được truy cập: " + changeInfo.url);
    
    // Gửi URL qua WebSocket
    sendDataToServer(changeInfo.url, localIP);
  }
});

// Gửi URL đến server WebSocket
function sendDataToServer(url, ip) {
  if (socket.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify({ url: url, ip: ip }));
    console.log("Dữ liệu đã được gửi qua WebSocket:", url);
  } else {
    console.error("Kết nối WebSocket không mở.");
  }
}

