import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

String apiBase = 'http://localhost:5265/api';

Future<void> initApi() async {
  final prefs = await SharedPreferences.getInstance();
  final savedUrl = prefs.getString('api_url');
  if (savedUrl != null) {
    apiBase = savedUrl;
  }
}

Future<void> setApiUrl(String url) async {
  final prefs = await SharedPreferences.getInstance();
  if (!url.endsWith('/api')) {
    url = '$url/api';
  }
  await prefs.setString('api_url', url);
  apiBase = url;
}

dynamic _normalizeList(dynamic data) {
  if (data is List) return data;
  if (data is Map && data['items'] is List) return data['items'];
  return <dynamic>[];
}

Map<String, dynamic> _normalizeSummon(Map<String, dynamic> s) {
  return {
    'id': s['id'],
    'status': s['status'],
    'summon_date': s['summonDate'] ?? s['summon_date'] ?? '',
    'summon_time': s['time'] ?? s['summon_time'] ?? '',
    'reason': s['reason'] ?? '',
    'location': s['location'] ?? '',
    'created_by': s['createdByName'] ?? s['created_by'] ?? '',
    'title': s['title'] ?? '',
    'createdAt': s['createdAt'] ?? '',
  };
}

Map<String, dynamic> _normalizeNotification(Map<String, dynamic> n) {
  return {
    'id': n['id'],
    'title': n['title'] ?? '',
    'message': n['message'] ?? n['content'] ?? '',
    'is_read': n['isRead'] ?? n['is_read'] ?? false,
    'createdAt': n['createdAtRaw'] ?? n['createdAt'] ?? '',
    'type': n['type'] ?? '',
  };
}

Map<String, dynamic> _normalizeDocument(Map<String, dynamic> d) {
  return {
    'id': d['id'],
    'name': d['fileName'] ?? d['name'] ?? '',
    'file_type': d['fileType'] ?? d['file_type'] ?? '',
    'file_path': d['filePath'] ?? d['file_path'] ?? '',
    'filePath': d['filePath'] ?? '',
    'fileType': d['fileType'] ?? '',
    'documentType': d['documentType'] ?? '',
    'status': d['status'] ?? '',
    'rejection_reason': d['rejectionReason'] ?? d['rejection_reason'],
    'rejectionReason': d['rejectionReason'],
    'createdAt': d['createdAt'] ?? '',
  };
}

Map<String, dynamic> _normalizeApplication(Map<String, dynamic> a) {
  return {
    'id': a['id'],
    'title': a['title'] ?? '',
    'description': a['content'] ?? a['description'] ?? '',
    'type': a['type'] ?? '',
    'status': a['status'] ?? '',
    'createdAt': a['createdAt'] ?? '',
  };
}

Map<String, dynamic> _normalizeAppointment(Map<String, dynamic> a) {
  return {
    'id': a['id'],
    'date': a['appointmentDate'] ?? a['date'] ?? '',
    'time': a['time'] ?? '',
    'employeeName': a['employeeName'] ?? '',
    'status': a['status'] ?? '',
    'purpose': a['purpose'] ?? '',
  };
}

class ApiService {
  static Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('token');
  }

  static Future<void> setToken(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('token', token);
  }

  static Future<void> clearToken() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('token');
  }

  static Future<dynamic> _doRequest(String endpoint, {Map<String, dynamic>? body, String method = 'GET'}) async {
    final token = await getToken();
    final uri = Uri.parse('$apiBase$endpoint');

    final headers = {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };

    try {
      http.Response response;
      if (method == 'GET') {
        response = await http.get(uri, headers: headers).timeout(const Duration(seconds: 30));
      } else if (method == 'POST') {
        response = await http.post(uri, headers: headers, body: jsonEncode(body ?? {})).timeout(const Duration(seconds: 30));
      } else if (method == 'PUT') {
        response = await http.put(uri, headers: headers, body: jsonEncode(body ?? {})).timeout(const Duration(seconds: 30));
      } else if (method == 'DELETE') {
        response = await http.delete(uri, headers: headers).timeout(const Duration(seconds: 30));
      } else {
        throw Exception('Unknown method: $method');
      }

      if (response.statusCode != 200 && response.statusCode != 201) {
        if (response.body.isEmpty) {
          throw Exception('Сервер вернул пустой ответ (${response.statusCode})');
        }
        try {
          final error = jsonDecode(response.body);
          throw Exception(error['message'] ?? 'Ошибка (${response.statusCode})');
        } catch (e) {
          if (e is Exception) rethrow;
          throw Exception('Ошибка парсинга ответа (${response.statusCode})');
        }
      }

      if (response.body.isEmpty) return {};
      return jsonDecode(response.body);
    } catch (e) {
      final msg = e.toString().toLowerCase();
      if (msg.contains('timeout') || msg.contains('timed out')) {
        throw Exception('Сервер не отвечает. Проверьте, что API запущен');
      }
      if (msg.contains('connection') || msg.contains('refused') || msg.contains('socket')) {
        throw Exception('Нет соединения с сервером. Проверьте адрес API');
      }
      rethrow;
    }
  }

  static Future<Map<String, dynamic>> login(String login, String password) async {
    try {
      final response = await http.post(
        Uri.parse('$apiBase/auth/login'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'login': login, 'password': password}),
      ).timeout(const Duration(seconds: 15));

      if (response.statusCode != 200) {
        if (response.body.isEmpty) throw Exception('Пустой ответ сервера (${response.statusCode})');
        final error = jsonDecode(response.body);
        throw Exception(error['message'] ?? 'Ошибка входа (${response.statusCode})');
      }

      final data = jsonDecode(response.body);
      if (data['token'] != null) {
        await setToken(data['token']);
      }
      return data as Map<String, dynamic>;
    } catch (e) {
      if (e.toString().contains('TimeoutException')) {
        throw Exception('Превышено время ожидания. Проверьте подключение к серверу');
      }
      if (e.toString().contains('SocketException') || e.toString().contains('HandshakeException')) {
        throw Exception('Не удалось подключиться к серверу. Проверьте адрес API');
      }
      rethrow;
    }
  }

  static Future<Map<String, dynamic>> getProfile() async {
    return await _doRequest('/profile');
  }

  static Future<List<dynamic>> getSummons() async {
    final data = await _doRequest('/summons/my');
    final list = _normalizeList(data);
    return list.map((s) => _normalizeSummon(s as Map<String, dynamic>)).toList();
  }

  static Future<Map<String, dynamic>?> getSummon(int id) async {
    try {
      final data = await _doRequest('/summons/$id');
      if (data is Map) return _normalizeSummon(data as Map<String, dynamic>);
      return null;
    } catch (_) {
      return null;
    }
  }

  static Future<void> markSummonRead(int id) async {
    try {
      await _doRequest('/summons/$id/read', method: 'POST');
    } catch (_) {}
  }

  static Future<List<dynamic>> getDocuments() async {
    final data = await _doRequest('/document');
    final list = _normalizeList(data);
    return list.map((d) => _normalizeDocument(d as Map<String, dynamic>)).toList();
  }

  static Future<Map<String, dynamic>> uploadDocument(String filePath, String name, {String? fileType}) async {
    final token = await getToken();
    final uri = Uri.parse('$apiBase/document/upload');
    final req = http.MultipartRequest('POST', uri);
    req.headers['Authorization'] = 'Bearer $token';
    req.files.add(await http.MultipartFile.fromPath('file', filePath));
    req.fields['documentType'] = fileType ?? 'other';

    final streamed = await req.send();
    final resp = await http.Response.fromStream(streamed);

    if (resp.statusCode != 200) {
      if (resp.body.isNotEmpty) {
        final err = jsonDecode(resp.body);
        throw Exception(err['message'] ?? 'Ошибка загрузки');
      }
      throw Exception('Ошибка загрузки');
    }

    return jsonDecode(resp.body) as Map<String, dynamic>;
  }

  static Future<Map<String, dynamic>> getFeed() async {
    final data = await _doRequest('/feed');
    if (data is! Map) return { 'user': {}, 'items': <dynamic>[] };
    return {
      'user': data['user'] ?? {},
      'items': (data['items'] as List?)?.map((n) => _normalizeNotification(n as Map<String, dynamic>)).toList() ?? <dynamic>[],
    };
  }

  static Future<void> markFeedRead(int id) async {
    await _doRequest('/feed/$id/read', method: 'POST');
  }

  static Future<List<dynamic>> getApplications() async {
    final data = await _doRequest('/application/my');
    final list = _normalizeList(data);
    return list.map((a) => _normalizeApplication(a as Map<String, dynamic>)).toList();
  }

  static Future<Map<String, dynamic>> createApplication(Map<String, dynamic> data) async {
    final result = await _doRequest('/application', body: {
      'title': data['title'] ?? '',
      'type': data['type'] ?? '',
      'content': data['description'] ?? data['content'] ?? '',
    }, method: 'POST');
    return result as Map<String, dynamic>;
  }

  static Future<List<dynamic>> getAppointments() async {
    final data = await _doRequest('/appointments/my');
    final list = _normalizeList(data);
    return list.map((a) => _normalizeAppointment(a as Map<String, dynamic>)).toList();
  }

  static Future<Map<String, dynamic>> createAppointment(Map<String, dynamic> data) async {
    final result = await _doRequest('/appointments', body: {
      'appointmentDate': data['date'] ?? '',
      'time': data['time'] ?? '',
      'purpose': data['reason'] ?? data['purpose'] ?? '',
    }, method: 'POST');
    return result as Map<String, dynamic>;
  }

  static Future<List<dynamic>> getNotifications() async {
    final data = await _doRequest('/notifications');
    final list = _normalizeList(data);
    return list.map((n) => _normalizeNotification(n as Map<String, dynamic>)).toList();
  }

  static Future<void> markNotificationRead(int id) async {
    await _doRequest('/notifications/$id/read', method: 'POST');
  }

  static Future<List<dynamic>> getCalendarEvents({int? month, int? year}) async {
    final now = DateTime.now();
    month ??= now.month;
    year ??= now.year;
    final data = await _doRequest('/calendar/events?month=$month&year=$year');
    return _normalizeList(data);
  }

  static Future<List<dynamic>> getTimeSlots(int eventId) async {
    final data = await _doRequest('/calendar/slots/$eventId');
    return _normalizeList(data);
  }

  static Future<void> bookSlot(int eventId, String time) async {
    await _doRequest('/calendar/book', body: {'eventId': eventId, 'time': time}, method: 'POST');
  }

  static Future<Map<String, dynamic>> getStatistics() async {
    return await _doRequest('/employee/statistics');
  }

  static Future<Map<String, dynamic>> changePassword(String currentPassword, String newPassword) async {
    return await _doRequest('/auth/change-password', body: {
      'currentPassword': currentPassword,
      'newPassword': newPassword,
    }, method: 'POST');
  }

  static Future<int> getUnreadNotificationsCount() async {
    try {
      final data = await _doRequest('/notifications/unread-count');
      return data['count'] ?? 0;
    } catch (_) {
      return 0;
    }
  }

  static Future<Map<String, dynamic>> uploadDocumentPhoto({
    required String type,
    required List<int> bytes,
    required String fileName,
  }) async {
    final token = await getToken();
    final uri = Uri.parse('$apiBase/profile/photo');
    final req = http.MultipartRequest('POST', uri);
    req.headers['Authorization'] = 'Bearer $token';
    req.files.add(http.MultipartFile.fromBytes('file', bytes, filename: fileName));
    req.fields['type'] = type;

    final streamed = await req.send();
    final resp = await http.Response.fromStream(streamed);

    if (resp.statusCode != 200) {
      if (resp.body.isNotEmpty) {
        final err = jsonDecode(resp.body);
        throw Exception(err['message'] ?? 'Ошибка загрузки');
      }
      throw Exception('Ошибка загрузки');
    }

    return jsonDecode(resp.body) as Map<String, dynamic>;
  }
}
