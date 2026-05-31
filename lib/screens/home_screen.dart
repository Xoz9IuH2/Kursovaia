import 'package:flutter/material.dart';
import 'package:mobile/services/api_service.dart';
import 'package:mobile/screens/profile_screen.dart';
import 'package:mobile/screens/appointments_screen.dart';
import 'package:mobile/screens/summons_screen.dart';
import 'package:mobile/screens/applications_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  Map<String, dynamic> _profile = {};
  List<dynamic> _events = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    try {
      final feedData = await ApiService.getFeed();
      if (!mounted) return;
      setState(() => _profile = feedData['user'] ?? {});
    } catch (_) {}

    try {
      final now = DateTime.now();
      final events = await ApiService.getCalendarEvents(
        month: now.month,
        year: now.year,
      );
      if (!mounted) return;
      setState(() => _events = events);
    } catch (_) {}

    if (mounted) setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    final today = DateTime.now();
    final dateStr = '${today.day} ${_month(today.month)} ${today.year}';
    final userName = _profile['name'] ?? 'Гость';

    return Scaffold(
      backgroundColor: const Color(0xFFF5F5F5),
      body: SafeArea(
        child: Column(
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: const BoxDecoration(
                color: Color(0xFF1A1A1A),
                borderRadius: BorderRadius.vertical(bottom: Radius.circular(16)),
              ),
              child: Row(
                children: [
                  const Icon(Icons.shield, color: Color(0xFFFF6B00), size: 32),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'ВОЕНКОМ',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: Color(0xFFFF6B00),
                            letterSpacing: 2,
                          ),
                        ),
                        Text(
                          'Здравствуйте, $userName!',
                          style: const TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        ),
                        Text(
                          dateStr,
                          style: const TextStyle(color: Colors.white70),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 40),
                ],
              ),
            ),
            Expanded(
              child: _loading
                  ? const Center(
                      child: CircularProgressIndicator(
                        color: Color(0xFFFF6B00),
                      ),
                    )
                  : RefreshIndicator(
                      onRefresh: _loadData,
                      child: ListView(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                        children: [
                          if (_events.isNotEmpty) ...[
                            const Padding(
                              padding: EdgeInsets.symmetric(vertical: 8),
                              child: Text(
                                'Мероприятия',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  color: Color(0xFF333333),
                                ),
                              ),
                            ),
                            ..._events.map(
                              (e) => _EventCard(event: e),
                            ),
                          ] else ...[
                            const Center(
                              child: Padding(
                                padding: EdgeInsets.only(top: 40),
                                child: Text(
                                  'Нет мероприятий',
                                  style: TextStyle(color: Colors.grey),
                                ),
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
            ),
            _buildBottomNav(),
          ],
        ),
      ),
    );
  }

  Widget _buildBottomNav() {
    return BottomNavigationBar(
      currentIndex: 0,
      type: BottomNavigationBarType.fixed,
      backgroundColor: const Color(0xFF1A1A1A),
      selectedItemColor: const Color(0xFFFF6B00),
      unselectedItemColor: Colors.white54,
      showUnselectedLabels: true,
      onTap: (i) {
        if (i == 0) return;
        if (i == 1) {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => const SummonsScreen()),
          );
        } else if (i == 2) {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => const ProfileScreen()),
          );
        } else if (i == 3) {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => const AppointmentsScreen()),
          );
        } else if (i == 4) {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => const ApplicationsScreen()),
          );
        }
      },
      items: const [
        BottomNavigationBarItem(
          icon: Icon(Icons.home),
          label: 'Главная',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.assignment),
          label: 'Повестки',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.person),
          label: 'Профиль',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.calendar_month),
          label: 'Запись',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.description),
          label: 'Заявления',
        ),
      ],
    );
  }

  String _month(int m) {
    const months = [
      '', 'января', 'февраля', 'марта', 'апреля', 'мая', 'июня',
      'июля', 'августа', 'сентября', 'октября', 'ноября', 'декабря'
    ];
    return months[m];
  }
}

class _EventCard extends StatelessWidget {
  final dynamic event;

  const _EventCard({required this.event});

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      color: Colors.white,
      elevation: 1,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(
          color: const Color(0xFFFF6B00).withValues(alpha: 0.3),
        ),
      ),
      child: ListTile(
        leading: const Icon(Icons.event, color: Color(0xFFFF6B00)),
        title: Text(
          event['title'] ?? '',
          style: const TextStyle(color: Color(0xFF333333)),
        ),
        subtitle: Text(
          '${event['date'] ?? ''}  ${event['startTime'] ?? ''} - ${event['endTime'] ?? ''}',
          style: const TextStyle(color: Colors.grey),
        ),
        trailing: Text(
          event['location'] ?? '',
          style: const TextStyle(color: Colors.grey, fontSize: 12),
        ),
      ),
    );
  }
}
