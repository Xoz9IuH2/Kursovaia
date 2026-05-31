import 'package:flutter/material.dart';
import 'package:mobile/services/api_service.dart';

class CalendarScreen extends StatefulWidget {
  const CalendarScreen({super.key});

  @override
  State<CalendarScreen> createState() => _CalendarScreenState();
}

class _CalendarScreenState extends State<CalendarScreen> {
  DateTime _selectedDate = DateTime.now();
  List<dynamic> _events = [];
  List<dynamic> _slots = [];
  bool _loading = true;
  int? _selectedEventId;
  String? _selectedTime;

  @override
  void initState() {
    super.initState();
    _loadEvents();
  }

  Future<void> _loadEvents() async {
    setState(() => _loading = true);
    try {
      final data = await ApiService.getCalendarEvents(month: _selectedDate.month, year: _selectedDate.year);
      if (!mounted) return;
      setState(() { _events = data; _loading = false; });
    } catch (_) { if (mounted) setState(() => _loading = false); }
  }

  Future<void> _loadSlots(int eventId) async {
    setState(() { _loading = true; _selectedEventId = eventId; });
    try {
      final data = await ApiService.getTimeSlots(eventId);
      if (!mounted) return;
      setState(() { _slots = data; _loading = false; });
    } catch (_) { if (mounted) setState(() => _loading = false); }
  }

  Future<void> _book() async {
    if (_selectedEventId == null || _selectedTime == null) return;
    setState(() => _loading = true);
    try {
      await ApiService.bookSlot(_selectedEventId!, _selectedTime!);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Запись подтверждена')));
      Navigator.pop(context);
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
    }
    if (mounted) setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Запись на приём'), backgroundColor: const Color(0xFF1A1A1A), foregroundColor: Colors.white),
      body: Column(children: [
        _CalendarGrid(selectedDate: _selectedDate, events: _events, onSelect: (date) {
          setState(() => _selectedDate = date);
          _loadEvents();
        }),
        const Divider(),
        Expanded(
          child: _events.isEmpty
            ? const Center(child: Text('Нет доступных дат'))
            : _selectedEventId == null
              ? ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: _events.length,
                  itemBuilder: (ctx, i) {
                    final e = _events[i];
                    final isAvailable = (e['bookedSlots'] ?? 0) < (e['maxSlots'] ?? 0);
                    return Card(
                      child: ListTile(
                        leading: const Icon(Icons.calendar_today),
                        title: Text(e['title'] ?? ''),
                        subtitle: Text('${e['date']} ${e['startTime']} - ${e['endTime']}'),
                        trailing: isAvailable 
                          ? TextButton(onPressed: () => _loadSlots(e['id']), child: const Text('Выбрать'))
                          : const Text('Нет мест', style: TextStyle(color: Colors.red)),
                      ),
                    );
                  },
                )
              : _loading 
                ? const Center(child: CircularProgressIndicator())
                : Column(children: [
                    const Padding(padding: EdgeInsets.all(16), child: Text('Доступное время:', style: TextStyle(fontSize: 16))),
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: _slots.map<Widget>((s) {
                        final isBooked = s['isBooked'] == true;
                        return ChoiceChip(
                          label: Text(s['time'] ?? ''),
                          selected: _selectedTime == s['time'],
                          onSelected: isBooked ? null : (selected) {
                            setState(() => _selectedTime = selected ? s['time'] : null);
                          },
                        );
                      }).toList(),
                    ),
                    const SizedBox(height: 16),
                    ElevatedButton(
                      onPressed: _selectedTime == null ? null : _book,
                      child: const Text('Записаться'),
                    ),
                  ]),
        ),
      ]),
    );
  }
}

class _CalendarGrid extends StatelessWidget {
  final DateTime selectedDate;
  final List<dynamic> events;
  final Function(DateTime) onSelect;

  const _CalendarGrid({required this.selectedDate, required this.events, required this.onSelect});

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final daysInMonth = DateTime(selectedDate.year, selectedDate.month + 1, 0).day;
    final firstWeekday = DateTime(selectedDate.year, selectedDate.month, 1).weekday;

    return Column(children: [
        Padding(
          padding: const EdgeInsets.all(8),
          child: Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
            IconButton(icon: const Icon(Icons.chevron_left), onPressed: () => onSelect(DateTime(selectedDate.year, selectedDate.month - 1))),
            Text('${_month(selectedDate.month)} ${selectedDate.year}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            IconButton(icon: const Icon(Icons.chevron_right), onPressed: () => onSelect(DateTime(selectedDate.year, selectedDate.month + 1))),
          ]),
        ),
      Row(mainAxisAlignment: MainAxisAlignment.spaceAround, children: ['Пн','Вт','Ср','Чт','Пт','Сб','Вс'].map((d) => Text(d, style: const TextStyle(fontWeight: FontWeight.bold))).toList()),
      const SizedBox(height: 8),
      GridView.builder(
        shrinkWrap: true,
        physics: const NeverScrollableScrollPhysics(),
        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 7, childAspectRatio: 1),
        itemCount: daysInMonth + firstWeekday - 1,
        itemBuilder: (ctx, i) {
          if (i < firstWeekday - 1) return const SizedBox();
          final day = i - firstWeekday + 2;
          if (day < 1 || day > daysInMonth) return const SizedBox();
          final date = DateTime(selectedDate.year, selectedDate.month, day);
          final isPast = date.isBefore(DateTime(now.year, now.month, now.day));
          final isToday = date.day == now.day && date.month == now.month && date.year == now.year;
          final hasEvent = events.any((e) => e['date'] == '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}');
          
          return InkWell(
            onTap: isPast ? null : () => onSelect(date),
            child: Container(
              margin: const EdgeInsets.all(2),
              decoration: BoxDecoration(
                color: isToday ? const Color(0xFFFF6B00) : (hasEvent && !isPast ? Colors.green : null),
                shape: BoxShape.circle,
              ),
              child: Center(child: Text('$day', style: TextStyle(color: isPast ? Colors.grey : Colors.white))),
            ),
          );
        },
      ),
    ]);
  }

  String _month(int m) {
    const months = ['', 'Январь', 'Февраль', 'Март', 'Апрель', 'Май', 'Июнь', 'Июль', 'Август', 'Сентябрь', 'Октябрь', 'Ноябрь', 'Декабрь'];
    return months[m];
  }
}