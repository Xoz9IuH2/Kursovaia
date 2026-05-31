import 'package:flutter/material.dart';
import 'package:mobile/services/api_service.dart';

class SummonsScreen extends StatefulWidget {
  const SummonsScreen({super.key});

  @override
  State<SummonsScreen> createState() => _SummonsScreenState();
}

class _SummonsScreenState extends State<SummonsScreen> {
  List<dynamic> _summons = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final data = await ApiService.getSummons();
      if (!mounted) return;
      setState(() {
        _summons = data;
        _loading = false;
      });
    } catch (_) {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _markRead(int id) async {
    try {
      await ApiService.markSummonRead(id);
      _load();
    } catch (_) {}
  }

  String _statusLabel(String status) {
    switch (status) {
      case 'sent': return 'Отправлена';
      case 'delivered': return 'Доставлена';
      case 'arrived': return 'Явился';
      case 'no-show': return 'Не явился';
      default: return status;
    }
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'sent': return Colors.orange;
      case 'delivered': return Colors.blue;
      case 'arrived': return Colors.green;
      case 'no-show': return Colors.red;
      default: return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Повестки'),
        backgroundColor: const Color(0xFF1A1A1A),
        foregroundColor: Colors.white,
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _summons.isEmpty
              ? const Center(child: Text('Повесток нет'))
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _summons.length,
                    itemBuilder: (ctx, i) {
                      final s = _summons[i];
                      final isRead = s['status'] == 'delivered' || s['status'] == 'arrived';
                      final hasSummonDate = s['summon_date']?.isNotEmpty == true;
                      final hasTime = s['summon_time']?.isNotEmpty == true;

                      return Card(
                        margin: const EdgeInsets.only(bottom: 12),
                        color: isRead ? Colors.white : Colors.orange.shade50,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                          side: BorderSide(
                            color: _statusColor(s['status'] ?? ''),
                            width: isRead ? 1 : 2,
                          ),
                        ),
                        child: InkWell(
                          onTap: isRead ? null : () => _markRead(s['id']),
                          borderRadius: BorderRadius.circular(12),
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  children: [
                                    Expanded(
                                      child: Text(
                                        s['title'] ?? 'Повестка',
                                        style: TextStyle(
                                          fontSize: 16,
                                          fontWeight: isRead ? FontWeight.normal : FontWeight.bold,
                                        ),
                                      ),
                                    ),
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                      decoration: BoxDecoration(
                                        color: _statusColor(s['status'] ?? ''),
                                        borderRadius: BorderRadius.circular(8),
                                      ),
                                      child: Text(
                                        _statusLabel(s['status'] ?? ''),
                                        style: const TextStyle(color: Colors.white, fontSize: 11),
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 8),
                                if (s['reason']?.isNotEmpty == true)
                                  Padding(
                                    padding: const EdgeInsets.only(bottom: 4),
                                    child: Text(
                                      'Причина: ${s['reason']}',
                                      style: TextStyle(color: Colors.grey.shade600, fontSize: 13),
                                    ),
                                  ),
                                if (s['location']?.isNotEmpty == true)
                                  Padding(
                                    padding: const EdgeInsets.only(bottom: 4),
                                    child: Text(
                                      'Место: ${s['location']}',
                                      style: TextStyle(color: Colors.grey.shade600, fontSize: 13),
                                    ),
                                  ),
                                Row(
                                  children: [
                                    if (hasSummonDate)
                                      Text(
                                        '${s['summon_date']}',
                                        style: TextStyle(color: Colors.grey.shade500, fontSize: 12),
                                      ),
                                    if (hasSummonDate && hasTime)
                                      Text(' в ', style: TextStyle(color: Colors.grey.shade500, fontSize: 12)),
                                    if (hasTime)
                                      Text(
                                        '${s['summon_time']}',
                                        style: TextStyle(color: Colors.grey.shade500, fontSize: 12),
                                      ),
                                  ],
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
