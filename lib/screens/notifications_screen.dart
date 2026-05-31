import 'package:flutter/material.dart';
import 'package:mobile/services/api_service.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});
  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<dynamic> _notifs = [];
  bool _loading = true;
  int _unreadCount = 0;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final data = await ApiService.getNotifications();
      final count = await ApiService.getUnreadNotificationsCount();
      if (!mounted) return;
      setState(() {
        _notifs = data;
        _unreadCount = count;
        _loading = false;
      });
    } catch (_) {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _markRead(int id) async {
    try {
      await ApiService.markNotificationRead(id);
      _load();
    } catch (_) {}
  }

  IconData _typeIcon(String type) {
    switch (type) {
      case 'document_verified':
        return Icons.check_circle;
      case 'document_rejected':
        return Icons.cancel;
      case 'summon':
        return Icons.assignment;
      case 'appointment':
        return Icons.calendar_today;
      default:
        return Icons.notifications;
    }
  }

  Color _typeColor(String type) {
    switch (type) {
      case 'document_verified':
        return Colors.green;
      case 'document_rejected':
        return Colors.red;
      case 'summon':
        return Colors.orange;
      case 'appointment':
        return Colors.blue;
      default:
        return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            const Text('Уведомления'),
            if (_unreadCount > 0) ...[
              const SizedBox(width: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                decoration: BoxDecoration(
                  color: Colors.red,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  '$_unreadCount',
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ],
        ),
        backgroundColor: const Color(0xFF1A1A1A),
        foregroundColor: Colors.white,
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _notifs.isEmpty
              ? const Center(child: Text('Нет уведомлений'))
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _notifs.length,
                    itemBuilder: (ctx, i) {
                      final n = _notifs[i];
                      final isRead = n['is_read'] == true;
                      final color = _typeColor(n['type'] ?? '');

                      return Card(
                        margin: const EdgeInsets.only(bottom: 8),
                        color: isRead
                            ? Colors.white
                            : color.withValues(alpha: 0.08),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                          side: isRead
                              ? BorderSide.none
                              : BorderSide(
                                  color: color.withValues(alpha: 0.3)),
                        ),
                        child: InkWell(
                          onTap: isRead ? null : () => _markRead(n['id']),
                          borderRadius: BorderRadius.circular(12),
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Icon(
                                  _typeIcon(n['type'] ?? ''),
                                  color: color,
                                  size: 28,
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        n['title'] ?? '',
                                        style: TextStyle(
                                          fontWeight: isRead
                                              ? FontWeight.normal
                                              : FontWeight.bold,
                                        ),
                                      ),
                                      const SizedBox(height: 4),
                                      Text(
                                        n['message'] ?? '',
                                        style: TextStyle(
                                          color: Colors.grey.shade600,
                                          fontSize: 13,
                                        ),
                                      ),
                                      if (n['createdAt']?.isNotEmpty == true) ...[
                                        const SizedBox(height: 4),
                                        Text(
                                          n['createdAt'],
                                          style: TextStyle(
                                            color: Colors.grey.shade400,
                                            fontSize: 11,
                                          ),
                                        ),
                                      ],
                                    ],
                                  ),
                                ),
                                if (!isRead)
                                  Container(
                                    width: 10,
                                    height: 10,
                                    decoration: BoxDecoration(
                                      color: color,
                                      shape: BoxShape.circle,
                                    ),
                                  ),
                              ],
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
