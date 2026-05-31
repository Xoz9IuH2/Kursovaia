import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mobile/services/api_service.dart';

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier();
});

class AuthState {
  final String? token;
  final Map<String, dynamic>? user;
  final bool loading;
  final String? error;

  AuthState({this.token, this.user, this.loading = false, this.error});

  bool get isAuthenticated => token != null;

  AuthState copyWith({
    String? token,
    Map<String, dynamic>? user,
    bool? loading,
    String? error,
  }) {
    return AuthState(
      token: token ?? this.token,
      user: user ?? this.user,
      loading: loading ?? this.loading,
      error: error,
    );
  }
}

class AuthNotifier extends StateNotifier<AuthState> {
  AuthNotifier() : super(AuthState()) {
    _init();
  }

  Future<void> _init() async {
    final token = await ApiService.getToken();
    if (token != null) {
      state = state.copyWith(token: token);
      await loadProfile();
    }
  }

  Future<void> login(String login, String password) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final data = await ApiService.login(login, password);
      state = state.copyWith(
        token: data['token'] as String?,
        user: data['user'] as Map<String, dynamic>?,
        loading: false,
      );
    } catch (e) {
      state = state.copyWith(
        loading: false,
        error: e.toString().replaceAll('Exception: ', ''),
      );
    }
  }

  Future<void> loadProfile() async {
    try {
      final data = await ApiService.getProfile();
      state = state.copyWith(user: data['user'] as Map<String, dynamic>?);
    } catch (_) {}
  }

  Future<void> logout() async {
    await ApiService.clearToken();
    state = AuthState();
  }
}

final profileProvider = FutureProvider<Map<String, dynamic>>((ref) async {
  final data = await ApiService.getProfile();
  return data['user'] as Map<String, dynamic>? ?? {};
});

final summonsProvider = FutureProvider<List<dynamic>>((ref) async {
  return await ApiService.getSummons();
});

final documentsProvider = FutureProvider<List<dynamic>>((ref) async {
  return await ApiService.getDocuments();
});

final notificationsProvider = FutureProvider<List<dynamic>>((ref) async {
  return await ApiService.getNotifications();
});

final applicationsProvider = FutureProvider<List<dynamic>>((ref) async {
  return await ApiService.getApplications();
});