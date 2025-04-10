<script setup lang="ts">
import { ref } from 'vue';
import Google from '@/assets/images/auth/social-google.svg';
const checkbox = ref(false);
const show1 = ref(false);
const password = ref('');
const code = ref('');
const email = ref('');
const Regform = ref();
const firstname = ref('');
const lastname = ref('');
const sending = ref(false);
const seconds = ref(0); 
const timer = ref<number | null>(null); // ID của timer
const passwordRules = ref([
  (v: string) => !!v || 'Password is required',
  (v: string) => (v && v.length <= 30) || 'Password must be less than 30 characters'
]);
const emailRules = ref([(v: string) => !!v || 'E-mail is required', (v: string) => /.+@.+\..+/.test(v) || 'E-mail must be valid']);

async function validate() {
  
    await CreateAccount(email, code)
  
}

function unwrapValue(value: any) {
      return value && typeof value === 'object' && 'value' in value ? value.value : value;
    }

// Gửi mã xác nhận qua email
async function sendCode() {
  if (!email.value || !/.+@.+\..+/.test(email.value)) {
    alert('Vui lòng nhập email hợp lệ trước khi gửi mã');
    return;
  }

  sending.value = true;
  
    await GetCode(email.value, password.value )
    // Giả lập gửi email
    await setTimeout(() => {
      sending.value = false;
      startCountdown();
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

async function GetCode(email: any, password: any) {
  try {
    // Validation cơ bản (có thể mở rộng thêm)
    if (!email || !password) {
      throw new Error('Email và password là bắt buộc');
    }

    const response = await fetch('http://localhost:5028/api/Auth/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password }),
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Lỗi server không xác định');
    }

    const result = await response.json();
    console.log('Đã gửi code thành công:', result);
    alert(`Đã gửi mã xác nhận đến ${email}`); // Sửa email.value thành email
    return result;

  } catch (error: any) {
    console.error('Lỗi gửi code:', error.message);
    if (error.message.includes("Email already exists")) {
      alert('Tài khoản đã tồn tại');
    } else {
      alert(`Lỗi gửi code: ${error.message}`);
    }
    throw error;
  }
}

async function CreateAccount(email: any, code: any) {
  try {
    if (!email || !password || !code) {
      throw new Error('Email , password và code là bắt buộc');
    }

    const response = await fetch('http://localhost:5028/api/Auth/verify', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ 
        email: unwrapValue(email), 
        code: unwrapValue(code) 
      }),
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Lỗi server không xác định');
    }

    // Thử parse JSON, nếu không được thì xử lý như text
    let result;
    const contentType = response.headers.get('Content-Type');
    
    if (contentType && contentType.includes('application/json')) {
      result = await response.json();
    } else {
      result = await response.text(); // Xử lý như plain text nếu không phải JSON
    }

    console.log('Tạo tài khoản thành công:', result);
    alert('Tài khoản đã được tạo thành công!');
    window.location.href = '/login1';
    return result;

  } catch (error: any) {
    console.error('Lỗi tạo tài khoản:', error.message);
    alert(`Lỗi tạo tài khoản: ${error.message}`);
    throw error;
  }
}
</script>

<template>
  
  <h5 class="text-h5 text-center my-4 mb-8">Sign up</h5>
  <v-form ref="Regform" lazy-validation action="/dashboards/analytical" class="mt-7 loginForm">
    <v-row>
      <v-col cols="12" sm="6">
        <v-text-field
          v-model="firstname"
          density="comfortable"
          hide-details="auto"
          variant="outlined"
          color="primary"
          label="Firstname"
        ></v-text-field>
      </v-col>
      <v-col cols="12" sm="6">
        <v-text-field
          v-model="lastname"
          density="comfortable"
          hide-details="auto"
          variant="outlined"
          color="primary"
          label="Lastname"
        ></v-text-field>
      </v-col>
    </v-row>
    <v-text-field
      v-model="email"
      :rules="emailRules"
      label="Email Address"
      class="mt-4 mb-4"
      required
      density="comfortable"
      hide-details="auto"
      variant="outlined"
      color="primary"
    ></v-text-field>
    <v-text-field
      v-model="password"
      :rules="passwordRules"
      label="Password"
      required
      density="comfortable"
      variant="outlined"
      color="primary"
      hide-details="auto"
      :append-icon="show1 ? '$eye' : '$eyeOff'"
      :type="show1 ? 'text' : 'password'"
      @click:append="show1 = !show1"
      class="pwdInput"
    ></v-text-field>

    <v-text-field 
    v-model="code"
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
    <div class="d-sm-inline-flex align-center mt-2 mb-7 mb-sm-0 font-weight-bold">
      <v-checkbox
        v-model="checkbox"
        :rules="[(v: any) => !!v || 'You must agree to continue!']"
        label="Agree with?"
        required
        color="primary"
        class="ms-n2"
        hide-details
      ></v-checkbox>
      <a href="#" class="ml-1 text-lightText">Terms and Condition</a>
    </div>
    <v-btn color="secondary" block class="mt-2" variant="flat" size="large" @click="validate()">Sign Up</v-btn>
  </v-form>
  <div class="mt-5 text-right">
    <v-divider />
    <v-btn variant="plain" to="/login1" class="mt-2 text-capitalize mr-n2">Already have an account?</v-btn>
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
</style>
