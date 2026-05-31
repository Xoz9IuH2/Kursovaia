import 'package:flutter/material.dart';
import 'package:mobile/services/api_service.dart';

class ApplicationsScreen extends StatefulWidget {
  const ApplicationsScreen({super.key});

  @override
  State<ApplicationsScreen> createState() => _ApplicationsScreenState();
}

class _ApplicationsScreenState extends State<ApplicationsScreen> {
  List<dynamic> _apps = [];
  bool _loading = true;

  static const List<Map<String, String>> predefinedTypes = [
    {
      'type': 'Заявление о постановке на воинский учёт',
      'desc': 'приехал в другой город, встать на учёт',
    },
    {
      'type': 'Заявление о замене военного билета',
      'desc': 'потерял, испортил, сменил фамилию',
    },
    {
      'type': 'Заявление об уточнении персональных данных',
      'desc': 'адрес, паспорт, семейное положение',
    },
    {
      'type': 'Заявление о выдаче справки',
      'desc': 'для ГИБДД, для работы, для суда',
    },
    {
      'type': 'Заявление об отсрочке от призыва',
      'desc': 'по учёбе, по здоровью (мед. справка), по семейным обстоятельствам',
    },
    {
      'type': 'Заявление о выдаче дубликата удостоверения',
      'desc': 'для ветеранов/участников',
    },
    {
      'type': 'Заявление о временном снятии с учёта',
      'desc': 'при отъезде за границу > 3 месяцев',
    },
  ];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final data = await ApiService.getApplications();
      if (!mounted) return;
      setState(() {
        _apps = data;
        _loading = false;
      });
    } catch (_) {
      if (mounted) setState(() => _loading = false);
    }
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'approved':
        return Colors.green;
      case 'rejected':
        return Colors.red;
      default:
        return Colors.orange;
    }
  }

  String _statusLabel(String status) {
    switch (status) {
      case 'approved':
        return 'Одобрено';
      case 'rejected':
        return 'Отклонено';
      default:
        return 'На рассмотрении';
    }
  }

  IconData _statusIcon(String status) {
    switch (status) {
      case 'approved':
        return Icons.check_circle;
      case 'rejected':
        return Icons.cancel;
      default:
        return Icons.hourglass_empty;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Заявления'),
        backgroundColor: const Color(0xFF1A1A1A),
        foregroundColor: Colors.white,
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: _createApp,
        backgroundColor: const Color(0xFFFF6B00),
        child: const Icon(Icons.add),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _apps.isEmpty
              ? const Center(child: Text('Нет заявлений'))
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _apps.length,
                    itemBuilder: (ctx, i) {
                      final a = _apps[i];
                      final color = _statusColor(a['status'] ?? '');
                      return Card(
                        margin: const EdgeInsets.only(bottom: 12),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                          side: BorderSide(
                              color: color.withValues(alpha: 0.5)),
                        ),
                        child: ListTile(
                          leading: Icon(
                            _statusIcon(a['status'] ?? ''),
                            color: color,
                          ),
                          title: Text(
                            a['title'] ?? '',
                            style: const TextStyle(fontWeight: FontWeight.w500),
                          ),
                          subtitle: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              if (a['description']?.isNotEmpty == true)
                                Text(a['description'] ?? ''),
                              if (a['createdAt']?.isNotEmpty == true)
                                Text(
                                  a['createdAt'],
                                  style: const TextStyle(
                                    color: Colors.grey,
                                    fontSize: 12,
                                  ),
                                ),
                            ],
                          ),
                          trailing: Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 10, vertical: 4),
                            decoration: BoxDecoration(
                              color: color.withValues(alpha: 0.2),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Text(
                              _statusLabel(a['status'] ?? ''),
                              style: TextStyle(
                                color: color,
                                fontSize: 12,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                ),
    );
  }

  Future<void> _createApp() async {
    String? selectedType;
    DateTime selectedDate = DateTime.now();
    String? selectedTime;

    final result = await showDialog<Map<String, dynamic>>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) {
          final now = DateTime.now();
          final daysInMonth = DateTime(selectedDate.year, selectedDate.month + 1, 0).day;
          final firstWeekday = DateTime(selectedDate.year, selectedDate.month, 1).weekday;

          return AlertDialog(
            title: const Text('Новое заявление'),
            content: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  DropdownButtonFormField<String>(
                    decoration: const InputDecoration(
                      labelText: 'Тип заявления',
                      border: OutlineInputBorder(),
                    ),
                    items: predefinedTypes.map((t) {
                      return DropdownMenuItem(
                        value: t['type'],
                        child: Text(t['type']!, style: const TextStyle(fontSize: 14)),
                      );
                    }).toList(),
                    onChanged: (v) => setDialogState(() {
                      selectedType = v;
                      selectedTime = null;
                    }),
                  ),
                  if (selectedType != null) ...[
                    const SizedBox(height: 16),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        IconButton(
                          icon: const Icon(Icons.chevron_left),
                          onPressed: () => setDialogState(() {
                            selectedDate = DateTime(selectedDate.year, selectedDate.month - 1);
                            selectedTime = null;
                          }),
                        ),
                        Text(
                          '${_month(selectedDate.month)} ${selectedDate.year}',
                          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                        ),
                        IconButton(
                          icon: const Icon(Icons.chevron_right),
                          onPressed: () => setDialogState(() {
                            selectedDate = DateTime(selectedDate.year, selectedDate.month + 1);
                            selectedTime = null;
                          }),
                        ),
                      ],
                    ),
                    Wrap(
                      spacing: 4,
                      runSpacing: 4,
                      children: List.generate(daysInMonth + firstWeekday - 1, (i) {
                        if (i < firstWeekday - 1) return const SizedBox(width: 40);
                        final day = i - firstWeekday + 2;
                        final date = DateTime(selectedDate.year, selectedDate.month, day);
                        final isPast = date.isBefore(DateTime(now.year, now.month, now.day));
                        final isSel = date.day == selectedDate.day && date.month == selectedDate.month;
                        return GestureDetector(
                          onTap: isPast ? null : () => setDialogState(() {
                            selectedDate = date;
                            selectedTime = null;
                          }),
                          child: Container(
                            width: 40, height: 40,
                            decoration: BoxDecoration(
                              color: isSel ? const Color(0xFFFF6B00) : null,
                              shape: BoxShape.circle,
                            ),
                            child: Center(child: Text('$day', style: TextStyle(
                              color: isSel ? Colors.white : (isPast ? Colors.grey : null),
                              fontWeight: isSel ? FontWeight.bold : null,
                            ))),
                          ),
                        );
                      }),
                    ),
                    const SizedBox(height: 16),
                    const Align(
                      alignment: Alignment.centerLeft,
                      child: Text('Время:', style: TextStyle(fontWeight: FontWeight.bold)),
                    ),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 6, runSpacing: 6,
                      children: ['09:00','10:00','11:00','12:00','13:00','14:00','15:00','16:00'].map((t) {
                        final isSel = selectedTime == t;
                        return ChoiceChip(
                          label: Text(t, style: const TextStyle(fontSize: 12)),
                          selected: isSel,
                          onSelected: (sel) => setDialogState(() => selectedTime = sel ? t : null),
                        );
                      }).toList(),
                    ),
                  ],
                ],
              ),
            ),
            actions: [
              TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Отмена')),
              ElevatedButton(
                onPressed: selectedType == null || selectedTime == null
                    ? null
                    : () {
                        final dateStr = '${selectedDate.year}-${selectedDate.month.toString().padLeft(2, '0')}-${selectedDate.day.toString().padLeft(2, '0')}';
                        WidgetsBinding.instance.addPostFrameCallback((_) {
                          Navigator.pop(ctx, {
                            'title': selectedType!,
                            'type': selectedType!,
                            'description': 'Дата: $dateStr, Время: $selectedTime',
                            'date': dateStr,
                            'time': selectedTime,
                          });
                        });
                      },
                child: const Text('Отправить'),
              ),
            ],
          );
        },
      ),
    );

    if (result != null && mounted) {
      setState(() => _loading = true);
      try {
        await ApiService.createApplication(result);
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Заявление отправлено')),
        );
        _load();
      } catch (e) {
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString().replaceAll('Exception: ', ''))),
        );
        setState(() => _loading = false);
      }
    }
  }

  String _month(int m) {
    const months = ['', 'Январь', 'Февраль', 'Март', 'Апрель', 'Май', 'Июнь',
      'Июль', 'Август', 'Сентябрь', 'Октябрь', 'Ноябрь', 'Декабрь'];
    return months[m];
  }
}
