import 'package:flutter/material.dart';
import 'package:mobile/services/api_service.dart';

class AppointmentsScreen extends StatefulWidget {
  const AppointmentsScreen({super.key});
  @override
  State<AppointmentsScreen> createState() => _AppointmentsScreenState();
}

class _AppointmentsScreenState extends State<AppointmentsScreen> {
  List<dynamic> _apps = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final data = await ApiService.getAppointments();
      if (!mounted) return;
      setState(() {
        _apps = data;
        _loading = false;
      });
    } catch (_) {
      if (mounted) setState(() => _loading = false);
    }
  }

  String _statusLabel(String status) {
    switch (status) {
      case 'confirmed':
        return 'Подтверждено';
      case 'completed':
        return 'Завершено';
      case 'cancelled':
        return 'Отменено';
      default:
        return 'Ожидает';
    }
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'confirmed':
        return Colors.green;
      case 'completed':
        return Colors.blue;
      case 'cancelled':
        return Colors.red;
      default:
        return Colors.orange;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Запись на приём'),
        backgroundColor: const Color(0xFF1A1A1A),
        foregroundColor: Colors.white,
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _showBookDialog,
        backgroundColor: const Color(0xFFFF6B00),
        icon: const Icon(Icons.add),
        label: const Text('Записаться'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _apps.isEmpty
              ? const Center(
                  child: Text('Нет записей'),
                )
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
                          side: BorderSide(color: color.withValues(alpha: 0.5)),
                        ),
                        child: ListTile(
                          leading: Icon(Icons.calendar_today, color: color),
                          title: Text(
                            '${a['purpose'] ?? 'Приём'}',
                            style: const TextStyle(fontWeight: FontWeight.w500),
                          ),
                          subtitle: Text(
                            '${a['date'] ?? ''} ${a['time'] ?? ''}',
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

  void _showBookDialog() {
    DateTime selectedDate = DateTime.now();
    String? selectedTime;

    showDialog(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) {
          final now = DateTime.now();
          final daysInMonth = DateTime(selectedDate.year, selectedDate.month + 1, 0).day;
          final firstWeekday = DateTime(selectedDate.year, selectedDate.month, 1).weekday;
          final isSunday = selectedDate.weekday == 7;

          List<String> availableTimes = [];
          if (!isSunday) {
            final isSaturday = selectedDate.weekday == 6;
            int startHour = isSaturday ? 10 : 8;
            int endHour = isSaturday ? 14 : 17;
            for (var h = startHour; h < endHour; h++) {
              availableTimes.add('${h.toString().padLeft(2, '0')}:00');
              if (!isSaturday || h < endHour - 1) {
                availableTimes.add('${h.toString().padLeft(2, '0')}:30');
              }
            }
            if (isSaturday) {
              availableTimes.removeWhere((t) {
                final parts = t.split(':');
                final hour = int.parse(parts[0]);
                return hour >= 14;
              });
            }
          }

          final isToday = selectedDate.year == now.year &&
              selectedDate.month == now.month &&
              selectedDate.day == now.day;
          if (isToday) {
            availableTimes = availableTimes.where((t) {
              final parts = t.split(':');
              final hour = int.parse(parts[0]);
              final minute = int.parse(parts[1]);
              return hour > now.hour || (hour == now.hour && minute > now.minute);
            }).toList();
          }

          return AlertDialog(
            title: const Text('Запись на приём'),
            content: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      IconButton(
                        icon: const Icon(Icons.chevron_left),
                        onPressed: () {
                          setDialogState(() {
                            selectedDate = DateTime(
                                selectedDate.year, selectedDate.month - 1);
                            selectedTime = null;
                          });
                        },
                      ),
                      Text(
                        '${_month(selectedDate.month)} ${selectedDate.year}',
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.chevron_right),
                        onPressed: () {
                          setDialogState(() {
                            selectedDate = DateTime(
                                selectedDate.year, selectedDate.month + 1);
                            selectedTime = null;
                          });
                        },
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 4,
                    runSpacing: 4,
                    children: List.generate(daysInMonth + firstWeekday - 1, (i) {
                      if (i < firstWeekday - 1) return const SizedBox(width: 40);
                      final day = i - firstWeekday + 2;
                      final date = DateTime(selectedDate.year, selectedDate.month, day);
                      final isPast = date.isBefore(DateTime(now.year, now.month, now.day));
                      final isSun = date.weekday == 7;
                      final isSel = date.day == selectedDate.day &&
                          date.month == selectedDate.month;
                      return GestureDetector(
                        onTap: (isPast || isSun)
                            ? null
                            : () => setDialogState(() {
                                  selectedDate = date;
                                  selectedTime = null;
                                }),
                        child: Container(
                          width: 40,
                          height: 40,
                          decoration: BoxDecoration(
                            color: isSel
                                ? const Color(0xFFFF6B00)
                                : (isSun || isPast
                                    ? Colors.grey.shade200
                                    : null),
                            shape: BoxShape.circle,
                          ),
                          child: Center(
                            child: Text(
                              '$day',
                              style: TextStyle(
                                color: isSel
                                    ? Colors.white
                                    : (isPast || isSun
                                        ? Colors.grey
                                        : null),
                                fontWeight:
                                    isSel ? FontWeight.bold : null,
                              ),
                            ),
                          ),
                        ),
                      );
                    }),
                  ),
                  if (isSunday) ...[
                    const SizedBox(height: 16),
                    const Text(
                      'Воскресенье — выходной',
                      style: TextStyle(color: Colors.red),
                    ),
                  ],
                  if (!isSunday && availableTimes.isNotEmpty) ...[
                    const SizedBox(height: 16),
                    const Align(
                      alignment: Alignment.centerLeft,
                      child: Text(
                        'Доступное время:',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                    ),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 6,
                      runSpacing: 6,
                      children: availableTimes.map((t) {
                        final isSel = selectedTime == t;
                        return ChoiceChip(
                          label: Text(t, style: const TextStyle(fontSize: 12)),
                          selected: isSel,
                          onSelected: (sel) =>
                              setDialogState(() => selectedTime = sel ? t : null),
                        );
                      }).toList(),
                    ),
                  ],
                ],
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(ctx),
                child: const Text('Отмена'),
              ),
              ElevatedButton(
                onPressed: selectedTime == null || isSunday
                    ? null
                    : () async {
                        final dateStr =
                            '${selectedDate.year}-${selectedDate.month.toString().padLeft(2, '0')}-${selectedDate.day.toString().padLeft(2, '0')}';
                        try {
                          await ApiService.createAppointment({
                            'date': dateStr,
                            'time': selectedTime,
                            'reason': 'Запись на приём',
                          });
                          if (ctx.mounted) Navigator.pop(ctx);
                          if (mounted) {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(
                                content: Text('Вы записаны на приём'),
                              ),
                            );
                            _load();
                          }
                        } catch (e) {
                          if (ctx.mounted) {
                            ScaffoldMessenger.of(ctx).showSnackBar(
                              SnackBar(content: Text(e.toString())),
                            );
                          }
                        }
                      },
                child: const Text('Записаться'),
              ),
            ],
          );
        },
      ),
    );
  }

  String _month(int m) {
    const months = [
      '', 'Январь', 'Февраль', 'Март', 'Апрель', 'Май', 'Июнь',
      'Июль', 'Август', 'Сентябрь', 'Октябрь', 'Ноябрь', 'Декабрь'
    ];
    return months[m];
  }
}
