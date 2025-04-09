import { defineStore } from 'pinia';

const baseUrl = `http://localhost:5028/api/Auth`;

export const fetchWrapper = {
    post,
  };
  
  async function post(url: string, body: any) {
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  
    const contentType = response.headers.get('content-type');
  
    if (!response.ok) {
      // Nếu lỗi, trả về thông báo từ server
      const errorText = await response.text();
      throw new Error(errorText);
    }
  
    if (contentType && contentType.includes('application/json')) {
      return await response.json(); // nếu là JSON thì parse
    } else {
      return await response.text(); // nếu là text thì trả text
    }
  }

export const useAuthStore = defineStore({
    id: 'auth-forgot',
    actions: {
      async sendpass(email: string) {
        try {
          const response = await fetchWrapper.post(`${baseUrl}/forgot-password`, { email });
          return response; // trả về kết quả để component xử lý nếu cần
        } catch (error) {
          console.error('Gửi yêu cầu quên mật khẩu thất bại:', error);
          throw error; // để component gọi hàm này có thể hiển thị thông báo lỗi
        }
      }
    }
  });

  