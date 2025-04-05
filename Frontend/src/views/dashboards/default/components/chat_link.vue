<script setup lang="ts">
import Chat_icon from '@/assets/images/icons/Chat.svg';
</script>

<template>
<v-card
    elevation="8"
    class="pa-10 rounded-xl d-flex flex-column align-center text-center gradient-border bg-secondary"
    color="transparent"
    block
    @click="$router.push('/ChatAi')"
  >
    <!-- Icon -->
    <div class="icon-container mb-4">
      <img :src="Chat_icon" width="25" />
    </div>

    <!-- Tiêu đề -->
    <h2 class="text-h8 font-weight-bold text-white mb-4">Trợ Lý AI</h2>

    <!-- Mô tả nhỏ -->
    <span class="text-subtitle-4 text-grey-lighten-2 mb-4">Tạo cuộc trò truyện với AI</span>

    <!-- Nội dung -->
    <span class="text-body-2 text-grey-lighten-3">
      Trợ lý AI giúp bạn trả lời các câu hỏi thắc mắc của bạn của bạn
    </span>
  </v-card>



</template>


<style scoped>
/* Viền Gradient ở dưới */
.gradient-border {
  border-image: linear-gradient(to right, #ff5e9a, #9b5eff);
  border-image-slice: 1;
}

/* Chỉnh icon */
.icon-container {
  width: 64px;
  height: 64px;
  background: linear-gradient(135deg, #ff5e9a, #9b5eff);
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>