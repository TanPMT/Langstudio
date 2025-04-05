<script setup lang="ts">
// imported components
import example_link from './components/exercise_link.vue';
import Chat_link from './components/chat_link.vue';
import check_link from './components/check_essay_link.vue';
</script>

<template>
  <v-row>
  <v-col cols="12" md="6">
    <example_link />
  </v-col>
  <v-col cols="12" md="6">
    <Chat_link />
  </v-col>
  <v-col cols="12" md="6">
    <check_link />
  </v-col>
  <v-col cols="12" md="6">
    <check_link />
  </v-col>
</v-row>
</template>
