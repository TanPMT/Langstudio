<script setup lang="ts">
import edit_icon from '@/assets/images/icons/edit_icon.svg';
</script>

<template>
<v-card
    elevation="8"
    class="pa-10 rounded-xl d-flex flex-column align-center text-center gradient-border bg-secondary"
    color="transparent"
    block
    @click=""
  >
    <!-- Icon -->
    <div class="icon-container mb-4">
      <img :src="edit_icon" width="25" />
    </div>

    <!-- Tiêu đề -->
    <h2 class="text-h8 font-weight-bold text-white mb-4">CHẤM ĐIỂM BÀI VIẾT</h2>

    <!-- Mô tả nhỏ -->
    <span class="text-subtitle-4 text-grey-lighten-2 mb-4">Trợ lý AI sẽ giúp bạn đánh giá bài viết</span>

    <!-- Nội dung -->
    <span class="text-body-2 text-grey-lighten-3">
      Nhận phản hối và gợi ý ngay lập tức.
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