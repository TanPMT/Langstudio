

<template>
  
  <v-btn color="primary" @click="toggleSidebar" class="ma-4 btn-history">
      History
  </v-btn>
  <div class="v-app div-all">
    <!-- Toggle Sidebar Button -->
    

    <!-- Sidebar as Popup -->
    <v-navigation-drawer v-model="sidebarOpen" temporary app width="320" class="elevation-2">
      <v-toolbar flat color="grey lighten-4">
        <v-toolbar-title class="font-weight-medium">Conversations</v-toolbar-title>
        
          <v-btn color="primary" @click="newChat" class="btn-newchat">
            New Conversation
          </v-btn>
        

        <v-btn icon @click="toggleSidebar" color="black">
          ✖
        </v-btn>
      </v-toolbar>
      
      <v-list dense nav class="py-0">
        <v-list-item 
          v-for="(chat, index) in chatHistory" 
          :key="chat.id" 
          @click="loadChat(index)"
          :class="{ 'grey lighten-3': currentChat === index }"
          two-line
        >
          <v-list-item-content>
            <v-list-item-title>{{ chat.title }}</v-list-item-title>
            <v-list-item-subtitle>{{ chat.preview }}</v-list-item-subtitle>
          </v-list-item-content>
          <v-list-item-action>
            <v-btn color="primary" @click.stop="deleteChat(index)" class="btn-del">
              Delete
            </v-btn>
          </v-list-item-action>
        </v-list-item>
      </v-list>
    </v-navigation-drawer>

    <!-- Main Content -->
    <div style="background: #f8fafc" class="v-main-div">
      <v-container fluid class="fill-height pa-0">
        <v-row class="fill-height" no-gutters>
          <v-col class="d-flex flex-column">
            <!-- Chat Messages -->
            <div class="messages-container" :style="{ maxHeight: messagesMaxHeight }">
              <div v-for="(message, index) in messages" :key="index" class="mb-6">
                <div 
                  class="message-bubble" 
                  :class="{ 'ml-auto user-message': message.role === 'user', 'mr-auto': message.role === 'ai' }"
                >
                  <div class="d-flex align-center mb-1">
                    <v-avatar size="32" :color="message.role === 'user' ? 'primary' : 'grey lighten-1'" class="mr-3">
                      <span class="white--text">{{ message.role === 'user' ? 'U' : 'AI' }}</span>
                    </v-avatar>
                    <strong>{{ message.role === 'user' ? 'You' : 'Assistant' }}</strong>
                  </div>
                  <p class="message-content">{{ message.content }}</p>
                </div>
              </div>
            </div>
          </v-col>
        </v-row>
      </v-container>
    </div>

    <div class="pa-4 textarea-container" style="background: white">
      <div
        ref="inputDiv"
        class="textarea"
        variant="outlined"
        contenteditable="true"
        @input="updateMessage"
        @keydown.enter.prevent="sendMessage"
        @paste="handlePaste"
        :placeholder="'Type your message here...'"
      ></div>

      <v-btn color="primary" elevation="0" @click="sendMessage" height="28" class="btn-send">
        Send
      </v-btn>
    </div>
  </div>
</template>

<script>

export default {
  name: 'ChatPopupSidebar',
  data: () => ({
    sidebarOpen: false,
    newMessage: '',
    messages: [
      { role: 'ai', content: 'Hello! How can I assist you today?' }
    ],
    chatHistory: [
      { id: 2, title: 'Conversation 2', preview: 'Type your mess...' },
      { id: 1, title: 'Conversation 1', preview: 'Hello! How can...' }
    ],
    currentChat: 0
  }),
  methods: {
    adjustHeight() {
      const inputDiv = this.$refs.inputDiv;
      const messagesContainer = document.querySelector('.messages-container');

      // Reset chiều cao của inputDiv trước
      inputDiv.style.height = 'auto';
      // Đặt chiều cao của inputDiv theo nội dung
      inputDiv.style.height = `${inputDiv.scrollHeight}px`;

      // Tính toán và cập nhật max-height cho messages-container
      const inputHeight = inputDiv.scrollHeight; // Chiều cao thực tế của inputDiv
      const baseHeight = 70; // Chiều cao cơ bản (66vh trong trường hợp của bạn)
      const calculatedHeight = baseHeight - inputHeight / 10; // Tính toán chiều cao giảm dần
      const minHeight = 55; // Giới hạn tối thiểu (ví dụ: 20vh)

      // Đảm bảo max-height không giảm dưới mức tối thiểu
      const newMaxHeight = Math.max(calculatedHeight, minHeight);
      messagesContainer.style.maxHeight = `${newMaxHeight}vh`;
    },
    updateMessage() {
      const inputDiv = this.$refs.inputDiv;
      this.newMessage = inputDiv.innerText.trim(); // Cập nhật newMessage từ nội dung div
      this.adjustHeight(); // Điều chỉnh chiều cao sau khi nội dung thay đổi
    },
    handlePaste(event) {
      // Ngăn chặn dán nội dung có định dạng, chỉ cho phép văn bản thuần
      event.preventDefault();
      const text = event.clipboardData.getData('text/plain');
      document.execCommand('insertText', false, text);
    },
    toggleSidebar() {
      this.sidebarOpen = !this.sidebarOpen;
    },
    sendMessage() {
      if (!this.newMessage.trim()) return;
      this.messages.push({ role: 'user', content: this.newMessage });
      this.chatHistory[this.currentChat].preview = this.newMessage.substring(0, 20) + '...';
      setTimeout(() => {
        const aiResponse = 'This is a simulated response to: ' + this.newMessage;
        this.messages.push({ role: 'ai', content: aiResponse });
        this.chatHistory[this.currentChat].preview = aiResponse.substring(0, 20) + '...';
      }, 500);
      this.newMessage = '';
      this.$nextTick(() => {
        const inputDiv = this.$refs.inputDiv;
        inputDiv.innerHTML = ''; // Xóa hoàn toàn nội dung, bao gồm các thẻ HTML
        inputDiv.style.height = 'auto'; // Reset chiều cao về mặc định
      });
      
    },
    newChat() {
      this.messages = [{ role: 'ai', content: 'Hello! How can I assist you today?' }];
      this.chatHistory.unshift({ 
        id: Date.now(),
        title: 'Conversation ' + (this.chatHistory.length + 1),
        preview: 'Hello! How can...'
      });
      this.currentChat = 0;
    },
    loadChat(index) {
      this.currentChat = index;
      this.messages = [
        { role: 'user', content: 'Previous message from chat ' + this.chatHistory[index].title },
        { role: 'ai', content: 'Response to chat ' + this.chatHistory[index].title }
      ];
    },
    deleteChat(index) {
      if (this.chatHistory.length === 1) {
        this.newChat();
      }
      this.chatHistory.splice(index, 1);
      if (this.currentChat === index) {
        this.currentChat = 0;
        this.loadChat(0);
      } else if (this.currentChat > index) {
        this.currentChat--;
      }
    }
  },
  mounted() {
    // Khởi tạo chiều cao ban đầu cho div
    this.adjustHeight();
  }
}
</script>

<style scoped>
.message-bubble {
  max-width: 70%;
  padding: 16px;
  border-radius: 12px;
  background: white;
  box-shadow: 0 2px 4px rgba(0,0,0,0.05);
  word-break: break-word;
}

.user-message {
  background: #e3f2fd;
}

.btn-newchat {
  margin: 1vh;
  padding: 1vh;
}

.btn-del {
  margin: 1vh;
  padding: 1vh;
}

.textarea-container {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  height: auto; /* Cho phép div cha mở rộng theo nội dung */
  border-radius: 10px;
}

.textarea {
  width: 100%;
  min-height: 32px; /* Chiều cao tối thiểu tương đương 1 dòng */
  max-height: 20vh;
  overflow-y: auto; /* Cho phép thanh cuộn khi nội dung vượt quá */
  background: white;
  border: 1px solid #c5c5c5; /* Viền giống v-textarea */
  border-radius: 10px;
  padding: 8px; /* Padding để giống textarea */
  outline: none; /* Loại bỏ viền mặc định khi focus */
  box-sizing: border-box;
}

/* Placeholder cho div contenteditable */
.textarea:empty:before {
  content: attr(placeholder);
  color: #999; /* Màu placeholder */
  pointer-events: none; /* Ngăn tương tác với placeholder */
}

.textarea:focus {
  border-color: #1976d2; /* Màu viền khi focus, giống v-textarea */
  box-shadow: 0 0 0 1px #1976d2; /* Hiệu ứng focus */
}

.btn-send {
  margin-top: 8px; /* Khoảng cách giữa div và nút Send */
}

.v-app {
  height: 84vh;
  overflow: hidden; /* Ngăn v-app mở rộng */
  display: flex;
  flex-direction: column;
}

.v-main-div {
  flex: 1; /* Đảm bảo v-main chiếm không gian còn lại */
  display: flex;
  flex-direction: column;
}

.btn-history {
  position: fixed;
  top:80px;
  right: 20px; /* Cách mép phải 20px */
  border-radius: 10px;
  opacity: 0.8;
}

.messages-container {
  flex-grow: 1;
  overflow-y: auto;
  padding: 16px;
  max-height: 70vh;
}

.div-all{
  border-radius: 10px;
}
</style>