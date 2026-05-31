import 'package:flutter/material.dart';
import 'package:mobile/services/api_service.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});
  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  final _urlController = TextEditingController();
  bool _loading = false;

  @override
  void initState() {
    super.initState();
    _urlController.text = apiBase;
  }

  Future<void> _save() async {
    setState(() => _loading = true);
    try {
      await setApiUrl(_urlController.text);
      if (!mounted) return;
      Navigator.pop(context);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Сохранено!')),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Ошибка: $e')),
      );
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Настройки')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('URL сервера', style: TextStyle(fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            TextField(controller: _urlController, decoration: const InputDecoration(hintText: 'https://example.com/api')),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _loading ? null : _save,
                style: ElevatedButton.styleFrom(backgroundColor: const Color(0xFFFF6B00)),
                child: _loading ? const CircularProgressIndicator() : const Text('Сохранить'),
              ),
            ),
            const SizedBox(height: 32),
            const Text('Инструкция:', style: TextStyle(fontWeight: FontWeight.bold)),
            const Text('1. Для эмулятора: http://10.0.2.2:5000/api\n2. Для телефона в той же сети: http://IP:5000/api\n3. Через интернет: используй ngrok или публичный IP'),
          ],
        ),
      ),
    );
  }
}