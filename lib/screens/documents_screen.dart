import 'package:flutter/material.dart';
import 'package:mobile/services/api_service.dart';
import 'package:file_picker/file_picker.dart';

enum DocumentType { passport, snils, militaryTicket, other }

class DocumentsScreen extends StatefulWidget {
  const DocumentsScreen({super.key});

  @override
  State<DocumentsScreen> createState() => _DocumentsScreenState();
}

class _DocumentsScreenState extends State<DocumentsScreen> {
  List<dynamic> _docs = [];
  bool _loading = true;
  DocumentType _selectedType = DocumentType.other;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final data = await ApiService.getDocuments();
      if (!mounted) return;
      setState(() { _docs = data; _loading = false; });
    } catch (_) { if (mounted) setState(() => _loading = false); }
  }

  Future<void> _upload() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.any,
      allowMultiple: false,
    );

    if (result == null || result.files.isEmpty) return;

    final file = result.files.first;
    if (file.path == null) return;

    final name = await _showNameDialog();
    if (name == null || name.isEmpty) return;

    setState(() => _loading = true);

    try {
      await ApiService.uploadDocument(file.path!, name, fileType: _typeToString(_selectedType));
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Документ загружен')),
      );
      _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString().replaceAll('Exception: ', ''))),
        );
      }
    }

    if (mounted) setState(() => _loading = false);
  }

  Future<String?> _showNameDialog() async {
    final controller = TextEditingController();
    return showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Название документа'),
        content: StatefulBuilder(
          builder: (ctx, setDialogState) => Column(mainAxisSize: MainAxisSize.min, children: [
              DropdownButtonFormField<DocumentType>(
              initialValue: _selectedType,
              decoration: const InputDecoration(labelText: 'Тип документа'),
              items: DocumentType.values.map((t) => DropdownMenuItem(
                value: t,
                child: Text(_typeLabel(t)),
              )).toList(),
              onChanged: (v) => setDialogState(() => _selectedType = v!),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: controller,
              decoration: const InputDecoration(labelText: 'Название'),
            ),
          ]),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Отмена')),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, controller.text),
            child: const Text('Загрузить'),
          ),
        ],
      ),
    );
  }

  String _typeLabel(DocumentType type) {
    switch (type) {
      case DocumentType.passport: return 'Паспорт';
      case DocumentType.snils: return 'СНИЛС';
      case DocumentType.militaryTicket: return 'Военный билет';
      case DocumentType.other: return 'Другое';
    }
  }

  String _typeToString(DocumentType type) {
    switch (type) {
      case DocumentType.passport: return 'passport';
      case DocumentType.snils: return 'snils';
      case DocumentType.militaryTicket: return 'military_ticket';
      case DocumentType.other: return 'other';
    }
  }

  IconData _typeIcon(String? fileType) {
    switch (fileType) {
      case 'passport': return Icons.badge;
      case 'snils': return Icons.card_membership;
      case 'military_ticket': return Icons.military_tech;
      default: return Icons.description;
    }
  }

  String _typeName(String? fileType) {
    switch (fileType) {
      case 'passport': return 'Паспорт';
      case 'snils': return 'СНИЛС';
      case 'military_ticket': return 'Военный билет';
      default: return 'Документ';
    }
  }

  IconData _statusIcon(String status) {
    switch (status) {
      case 'approved': case 'verified': return Icons.check_circle;
      case 'rejected': return Icons.cancel;
      case 'pending': return Icons.hourglass_empty;
      default: return Icons.help_outline;
    }
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'approved': case 'verified': return Colors.green;
      case 'rejected': return Colors.red;
      case 'pending': return Colors.orange;
      default: return Colors.grey;
    }
  }

  String _statusLabel(String status) {
    switch (status) {
      case 'approved': case 'verified': return 'Верифицирован';
      case 'rejected': return 'Отклонён';
      case 'pending': return 'На проверке';
      default: return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Документы'), backgroundColor: const Color(0xFF1A1A1A), foregroundColor: Colors.white),
      body: _loading
        ? const Center(child: CircularProgressIndicator())
        : _docs.isEmpty
          ? Center(child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
              const Icon(Icons.folder_open, size: 64, color: Colors.grey),
              const SizedBox(height: 16),
              const Text('Нет документов'),
              const SizedBox(height: 8),
              const Text('Загрузите документы для верификации', style: TextStyle(color: Colors.grey)),
            ]))
          : ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: _docs.length,
              itemBuilder: (ctx, i) {
                final d = _docs[i];
                return _DocCard(
                  name: d['name'] ?? '',
                  fileType: d['file_type'] ?? 'other',
                  filePath: d['file_path'] ?? '',
                  typeIcon: _typeIcon(d['file_type']),
                  typeName: _typeName(d['file_type']),
                  status: d['status'] ?? 'pending',
                  statusIcon: _statusIcon(d['status'] ?? 'pending'),
                  statusColor: _statusColor(d['status'] ?? 'pending'),
                  statusLabel: _statusLabel(d['status'] ?? 'pending'),
                  rejectionReason: d['rejection_reason'],
                  verifiedAt: d['verified_at'],
                );
              },
            ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _upload,
        backgroundColor: const Color(0xFFFF6B00),
        icon: const Icon(Icons.upload),
        label: const Text('Загрузить'),
      ),
    );
  }
}

class _DocCard extends StatelessWidget {
  final String name;
  final String fileType;
  final String filePath;
  final IconData typeIcon;
  final String typeName;
  final String status;
  final IconData statusIcon;
  final Color statusColor;
  final String statusLabel;
  final String? rejectionReason;
  final String? verifiedAt;

  const _DocCard({
    required this.name,
    required this.fileType,
    required this.filePath,
    required this.typeIcon,
    required this.typeName,
    required this.status,
    required this.statusIcon,
    required this.statusColor,
    required this.statusLabel,
    this.rejectionReason,
    this.verifiedAt,
  });

  @override
  Widget build(BuildContext context) {
    final needsVerification = status == 'pending';

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      color: needsVerification ? Colors.orange.withValues(alpha: 0.1) : null,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: needsVerification ? const BorderSide(color: Colors.orange, width: 1) : BorderSide.none,
      ),
      child: Column(
        children: [
          ListTile(
            leading: Icon(typeIcon, color: statusColor),
            title: Text(name),
            subtitle: Text(typeName),
            trailing: Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              decoration: BoxDecoration(
                color: statusColor.withValues(alpha: 0.2),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Row(mainAxisSize: MainAxisSize.min, children: [
                Icon(statusIcon, size: 16, color: statusColor),
                const SizedBox(width: 4),
                Text(statusLabel, style: TextStyle(color: statusColor, fontWeight: FontWeight.w500)),
              ]),
            ),
          ),
          if (rejectionReason != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              margin: const EdgeInsets.symmetric(horizontal: 12),
              decoration: BoxDecoration(
                color: Colors.red.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(children: [
                const Icon(Icons.warning, color: Colors.red, size: 16),
                const SizedBox(width: 8),
                Expanded(child: Text(rejectionReason!, style: const TextStyle(color: Colors.red))),
              ]),
            ),
          if (filePath.isNotEmpty)
            Padding(
              padding: const EdgeInsets.all(12),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: Image.network(
                  filePath.startsWith('http') ? filePath : '$apiBase/$filePath',
                  height: 200,
                  width: double.infinity,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) => Container(
                    height: 100,
                    color: Colors.grey.shade200,
                    child: const Center(child: Icon(Icons.broken_image, size: 40)),
                  ),
                  loadingBuilder: (context, child, progress) {
                    if (progress == null) return child;
                    return Container(
                      height: 200,
                      color: Colors.grey.shade100,
                      child: const Center(child: CircularProgressIndicator()),
                    );
                  },
                ),
              ),
            ),
          const SizedBox(height: 8),
        ],
      ),
    );
  }
}