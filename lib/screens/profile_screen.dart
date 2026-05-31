import 'package:flutter/material.dart';
import 'package:file_picker/file_picker.dart';
import 'package:mobile/services/api_service.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  Map<String, dynamic> _profile = {};
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final data = await ApiService.getProfile();
      if (!mounted) return;
      setState(() {
        _profile = data['user'] ?? data;
        _loading = false;
      });
    } catch (_) {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _uploadPhoto(String type) async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.image,
      allowMultiple: false,
      withData: true,
    );
    if (result == null || result.files.isEmpty) return;
    final file = result.files.first;
    if (file.bytes == null) return;

    setState(() => _loading = true);
    try {
      await ApiService.uploadDocumentPhoto(
        type: type,
        bytes: file.bytes!,
        fileName: file.name,
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Фото отправлено на верификацию')),
      );
      _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString().replaceAll('Exception: ', ''))),
        );
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _changePassword() async {
    final currentCtrl = TextEditingController();
    final newCtrl = TextEditingController();
    final confirmCtrl = TextEditingController();

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Смена пароля'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: currentCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Текущий пароль'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: newCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Новый пароль'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: confirmCtrl,
                obscureText: true,
                decoration:
                    const InputDecoration(labelText: 'Подтвердите пароль'),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Отмена'),
          ),
          ElevatedButton(
            onPressed: () async {
              if (newCtrl.text.length < 8) {
                ScaffoldMessenger.of(ctx).showSnackBar(
                  const SnackBar(
                    content: Text('Пароль должен быть минимум 8 символов'),
                  ),
                );
                return;
              }
              if (newCtrl.text != confirmCtrl.text) {
                ScaffoldMessenger.of(ctx).showSnackBar(
                  const SnackBar(content: Text('Пароли не совпадают')),
                );
                return;
              }
              try {
                await ApiService.changePassword(
                    currentCtrl.text, newCtrl.text);
                if (ctx.mounted) Navigator.pop(ctx, true);
              } catch (e) {
                if (ctx.mounted) {
                  ScaffoldMessenger.of(ctx).showSnackBar(
                    SnackBar(content: Text(e.toString())),
                  );
                }
              }
            },
            child: const Text('Сменить'),
          ),
        ],
      ),
    );

    if (result == true && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Пароль изменён')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Профиль'),
        backgroundColor: const Color(0xFF1A1A1A),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.settings),
            onPressed: () => _changePassword(),
            tooltip: 'Сменить пароль',
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _Section(title: 'Личные данные', children: [
                    _Field(label: 'ФИО', value: _profile['fio'] ?? '—'),
                    _Field(
                        label: 'Дата рождения',
                        value: _profile['dateOfBirth'] ?? '—'),
                    _Field(
                        label: 'Адрес регистрации',
                        value: _profile['registrationAddress'] ?? '—'),
                    _Field(
                        label: 'Адрес проживания',
                        value: _profile['residenceAddress'] ?? '—'),
                  ]),
                  const SizedBox(height: 24),
                  _Section(title: 'Паспорт', children: [
                    _Field(
                        label: 'Серия',
                        value: _profile['passportSeries'] ?? '—'),
                    _Field(
                        label: 'Номер',
                        value: _profile['passportNumber'] ?? '—'),
                    _Field(
                        label: 'Кем выдан',
                        value: _profile['passportIssued'] ?? '—'),
                    _Field(
                        label: 'Дата выдачи',
                        value: _profile['passportDate'] ?? '—'),
                    const SizedBox(height: 12),
                    _PhotoBlock(
                      photoPath: _profile['passportPhotoPath'] as String?,
                      photoStatus: _profile['passportPhotoStatus'] as String?,
                      label: 'Фото паспорта',
                      onUpload: () => _uploadPhoto('passport'),
                    ),
                  ]),
                  const SizedBox(height: 24),
                  _Section(title: 'Военный билет', children: [
                    _Field(
                        label: 'Номер',
                        value: _profile['militaryTicketNumber'] ?? '—'),
                    _Field(
                        label: 'Категория годности',
                        value: _profile['fitnessCategory'] ?? '—'),
                    _Field(
                        label: 'Статус учёта',
                        value: _profile['accountStatus'] ?? '—'),
                    const SizedBox(height: 12),
                    _PhotoBlock(
                      photoPath: _profile['militaryPhotoPath'] as String?,
                      photoStatus: _profile['militaryPhotoStatus'] as String?,
                      label: 'Фото военного билета',
                      onUpload: () => _uploadPhoto('military'),
                    ),
                  ]),
                  const SizedBox(height: 24),
                  _Section(title: 'Контакты', children: [
                    _Field(
                        label: 'Телефон',
                        value: _profile['phone'] ?? '—'),
                    _Field(
                        label: 'Email',
                        value: _profile['email'] ?? '—'),
                  ]),
                  const SizedBox(height: 12),
                  SizedBox(
                    width: double.infinity,
                    child: OutlinedButton.icon(
                      icon: const Icon(Icons.logout),
                      label: const Text('Выйти'),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: Colors.red,
                      ),
                      onPressed: () async {
                        await ApiService.clearToken();
                        if (!mounted) return;
                        Navigator.of(context)
                            .pushNamedAndRemoveUntil('/login', (route) => false);
                      },
                    ),
                  ),
                  const SizedBox(height: 32),
                ],
              ),
            ),
    );
  }
}

class _PhotoBlock extends StatelessWidget {
  final String? photoPath;
  final String? photoStatus;
  final String label;
  final VoidCallback onUpload;

  const _PhotoBlock({
    required this.photoPath,
    required this.photoStatus,
    required this.label,
    required this.onUpload,
  });

  @override
  Widget build(BuildContext context) {
    final status = photoStatus ?? 'none';

    Color borderColor;
    String statusText;
    IconData statusIcon;
    Color statusColor;
    bool showUploadButton = false;

    switch (status) {
      case 'verified':
        borderColor = Colors.green;
        statusText = 'Верифицировано';
        statusIcon = Icons.check_circle;
        statusColor = Colors.green;
        break;
      case 'rejected':
        borderColor = Colors.red;
        statusText = 'Отказано';
        statusIcon = Icons.cancel;
        statusColor = Colors.red;
        showUploadButton = true;
        break;
      case 'pending':
        borderColor = Colors.orange;
        statusText = 'Ожидает';
        statusIcon = Icons.hourglass_empty;
        statusColor = Colors.orange;
        break;
      default:
        borderColor = Colors.grey;
        statusText = 'Фото не загружено';
        statusIcon = Icons.add_a_photo;
        statusColor = Colors.grey;
        showUploadButton = true;
    }

    return Column(
      children: [
        Container(
          width: double.infinity,
          height: 140,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: borderColor, width: 2),
            color: Colors.grey.shade50,
          ),
          clipBehavior: Clip.antiAlias,
          child: photoPath != null && photoPath!.isNotEmpty
              ? Image.network(
                  photoPath!.startsWith('http')
                      ? photoPath!
                      : '${apiBase.replaceAll('/api', '').replaceAll(RegExp(r'/+$'), '')}$photoPath',
                  fit: BoxFit.cover,
                  width: double.infinity,
                  height: 140,
                  errorBuilder: (context, error, stackTrace) => Center(
                    child: Icon(Icons.broken_image, size: 48, color: Colors.grey.shade300),
                  ),
                )
              : Center(
                  child: Icon(Icons.image, size: 48, color: Colors.grey.shade300),
                ),
        ),
        const SizedBox(height: 8),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(statusIcon, size: 14, color: statusColor),
            const SizedBox(width: 4),
            Text(
              statusText,
              style: TextStyle(
                color: statusColor,
                fontSize: 12,
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
        if (showUploadButton) ...[
          const SizedBox(height: 8),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              icon: const Icon(Icons.camera_alt, size: 16),
              label: Text(
                status == 'rejected'
                    ? 'Загрузить новое фото'
                    : 'Загрузить фото',
                style: const TextStyle(fontSize: 13),
              ),
              style: OutlinedButton.styleFrom(
                foregroundColor: const Color(0xFFFF6B00),
                side: const BorderSide(color: Color(0xFFFF6B00)),
                padding: const EdgeInsets.symmetric(vertical: 8),
              ),
              onPressed: onUpload,
            ),
          ),
        ],
      ],
    );
  }
}

class _Section extends StatelessWidget {
  final String title;
  final List<Widget> children;

  const _Section({required this.title, required this.children});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: const TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: Color(0xFFFF6B00),
          ),
        ),
        const SizedBox(height: 12),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Column(children: children),
        ),
      ],
    );
  }
}

class _Field extends StatelessWidget {
  final String label, value;

  const _Field({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 140,
            child: Text(label, style: const TextStyle(color: Colors.grey)),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(fontWeight: FontWeight.w500),
            ),
          ),
        ],
      ),
    );
  }
}
