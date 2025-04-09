<script setup lang="ts">
import { ref } from 'vue';
import { Form } from 'vee-validate';
import { useAuthStore } from '@/stores/auth-forgotpass';
const checkbox = ref(false);
const valid = ref(false);
const show1 = ref(false);
//const logform = ref();
const email = ref('thai25800852@gmail.com');
const emailRules = ref([(v: string) => !!v || 'E-mail is required', (v: string) => /.+@.+\..+/.test(v) || 'E-mail must be valid']);
const authStore = useAuthStore();
const seconds = ref(0);   // Thời gian đếm ngược
const timer = ref<number | null>(null); // ID của timer
const sending = ref(false); // Trạng thái gửi email

// Gửi mã xác nhận qua email
function sendCode() {
  if (!email.value || !/.+@.+\..+/.test(email.value)) {
    alert('Vui lòng nhập email hợp lệ trước khi gửi mã');
    return;
  }

  sending.value = true;

  // Giả lập gửi email
  setTimeout(() => {
    sending.value = false;
    startCountdown();
    alert(`Đã gửi mã xác nhận đến ${email.value}`);
  }, 1000);
}

// Bắt đầu đếm ngược 60 giây
function startCountdown() {
  seconds.value = 60;
  if (timer.value) clearInterval(timer.value);
  timer.value = setInterval(() => {
    if (seconds.value > 0) {
      seconds.value--;
    } else {
      clearInterval(timer.value!);
    }
  }, 1000);
}



// Gửi mã xác nhận và bắt đầu đếm ngược
async function sendpassacction() {
  // if (!email.value || !/.+@.+\..+/.test(email.value)) {
  //   alert('Vui lòng nhập email hợp lệ trước khi gửi mã');
  //   return;
  // }


  try {
    console.log('Sending Resend-Password request:', {email: email.value});
    await authStore.sendpass(email.value);
    alert(`Đã gửi mật khẩu đến gmail ${email.value}`);
  } catch (error) {
    alert('Gửi mật khẩu đến gmail thất bại!');
  } finally {
    sending.value = false;
  }
}


</script>



<template>
  
  <h5 class="text-h5 text-center my-4 mb-8">Forgot Password</h5>
  <Form @submit="sendpassacction" class="mt-7 loginForm" v-slot="{ errors, isSubmitting }">
    <v-text-field
      v-model="email"
      :rules="emailRules"
      label="Email Address"
      class="mt-4 mb-8"
      required
      density="comfortable"
      hide-details="auto"
      variant="outlined"
      color="primary"
    ></v-text-field>
    <v-text-field 
    label="Code"
    required
    density="comfortable"
    variant="outlined"
    color="primary"
    hide-details="auto"
    class="mt-4 mb-8"
  >
    <template #append>
      <v-btn
        
        color="primary"
        class="mt-2"
        size="small"
        variant="text"
        :disabled="seconds > 0 || sending"
        @click="sendCode"
      >
        <span v-if="seconds === 0 && !sending">Send Code</span>
        <span v-else-if="sending">Sending...</span>
        <span v-else>{{ seconds }}s</span>
      </v-btn>
    </template>
  </v-text-field>

    <div class="d-sm-flex align-center mt-2 mb-7 mb-sm-0">
      <div class="ml-auto">
        <v-btn variant="plain" to="/login1" class="text-primary text-decoration-none">Login Page</v-btn>
      </div>
    </div>
    <v-btn color="secondary" :loading="isSubmitting" block class="mt-2" variant="flat" size="large" :disabled="valid" type="submit">
      Send new password</v-btn
    >
    <div v-if="errors.apiError" class="mt-2">
      <v-alert color="error">{{ errors.apiError }}</v-alert>
    </div>
  </Form>
  <div class="mt-5 text-right">
    <v-divider />
    <v-btn variant="plain" to="/register" class="mt-2 text-capitalize mr-n2">Don't Have an account?</v-btn>
  </div>
</template>
<style lang="scss">
.custom-devider {
  border-color: rgba(0, 0, 0, 0.08) !important;
}
.googleBtn {
  border-color: rgba(0, 0, 0, 0.08);
  margin: 30px 0 20px 0;
}
.outlinedInput .v-field {
  border: 1px solid rgba(0, 0, 0, 0.08);
  box-shadow: none;
}
.orbtn {
  padding: 2px 40px;
  border-color: rgba(0, 0, 0, 0.08);
  margin: 20px 15px;
}
.pwdInput {
  position: relative;
  .v-input__append {
    position: absolute;
    right: 10px;
    top: 50%;
    transform: translateY(-50%);
  }
}
.loginForm {
  .v-text-field .v-field--active input {
    font-weight: 500;
  }
}
</style>
