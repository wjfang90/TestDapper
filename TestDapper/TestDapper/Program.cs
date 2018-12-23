using System;
using Dapper;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;

namespace TestDapper
{
    class Program
    {
        private static readonly string connStr = @"Data Source=FANG-PC\SQLEXPRESS;User ID=sa;Password=sa;Initial Catalog=TestDapper;Pooling=true;Max Pool Size=100;";
        static void Main(string[] args)
        {
            //插入
            var contentInsert = new Content()
            {
                title = "标题1",
                content = "内容1"
            };
            //var resInsert = InsertContent(contentInsert);
            //Console.WriteLine($"{resInsert}条数据插入！！");

            var listInsert = new List<Content>()
            {
                new Content(){title="标题a",content="内容a"},
                new Content(){title="标题b",content="内容b"},
                new Content(){title="标题c",content="内容c"}
            };
            //var resMultInsert = InsertContent(listInsert);
            //Console.WriteLine($"{resMultInsert}条数据插入！！！");




            //更新
            var contentUpdate = new Content()
            {
                id = 1,
                title = "标题1",
                content = "内容1"
            };
            //var resUpdate = UpdateContent(contentUpdate);
            //Console.WriteLine($"{resUpdate}条数据已修改！！！");

            var listUpdate = new List<Content>()
            {
                new Content(){id=2,title="标题aa",content="内容aa"},
                new Content(){id=3,title="标题bb",content="内容bb"},
                new Content(){id=4,title="标题cc",content="内容cc"}
            };
            //var resMultUpdate = UpdateContent(listUpdate);
            //Console.WriteLine($"{resMultUpdate}条数据已修改！！！");



            //删除
            //Console.WriteLine($"{DeleteContent(new Content() { id = 10 })}条数据已删除");

            var listDelete = new List<Content>()
            {
                new Content() { id = 8 },
                new Content() { id = 9 }
            };
            Console.WriteLine($"{DeleteContent(listDelete)}条数据已删除");



            //查询
            Console.WriteLine($"########################查询#############################");
            var content = GetContent(1);
            Console.WriteLine($"id={content.id},title={content.title},content={content.content}");

            var contentList = GetContentList(new int[] { 1, 2, 3 });
            contentList.ForEach(c =>
            {
                Console.WriteLine($"id={c.id},title={c.title},content={c.content}");
            });


            //多条sql一起执行
            Console.WriteLine($"########################多条sql一起执行#############################");
            var conentWithComment = GetContentWithComment(1);

            Console.WriteLine($"文章：id={conentWithComment.id},title={conentWithComment.title},content={conentWithComment.content},");
            conentWithComment.comments.ForEach(c =>
            {
                Console.WriteLine($"评论：id={c.id},content={c.comment},content_id={c.content_id}");
            });


            //关联查询
            Console.WriteLine($"########################关联查询 1对1#############################");
            var comment = GetComment(1);
            Console.WriteLine($"文章：id={comment.CommentContent.id},title={comment.CommentContent.title},content={comment.CommentContent.content}");
            Console.WriteLine($"评论：id={comment.id},content={comment.comment}");


            Console.WriteLine($"########################关联查询 1对多#############################");
            var contents = GetCotents();

            contents.ForEach(t =>
            {
                Console.WriteLine($"文章：id={t.id},title={t.title},content={t.content}");
                t.Comments.ForEach(c =>
                {
                    Console.WriteLine($"评论：id={c.id},content={c.comment},content_id={c.content_id}");
                });
            });

            Console.ReadKey();
        }

        static int InsertContent(Content model)
        {
            string sql_insert = @"INSERT INTO [Content] (title, [content], status, add_time, modify_time)
                                  VALUES   (@title,@content,@status,@add_time,@modify_time)";
            return ExecuteNonQuery(sql_insert, model);
        }

        static int InsertContent(List<Content> modelList)
        {
            string sql_insert = @"INSERT INTO [Content] (title, [content], status, add_time, modify_time)
                                  VALUES   (@title,@content,@status,@add_time,@modify_time)";
            return ExecuteNonQuery(sql_insert, modelList);
        }

        static int UpdateContent(Content model)
        {
            string sql_update = @"UPDATE  [Content]
                                  SET    title = @title, [content] = @content, modify_time = GETDATE()
                                  WHERE   (id = @id)";
            return ExecuteNonQuery(sql_update, model);
        }

        static int UpdateContent(List<Content> model)
        {
            string sql_update = @"UPDATE  [Content]
                                  SET    title = @title, [content] = @content, modify_time = GETDATE()
                                  WHERE   (id = @id)";
            return ExecuteNonQuery(sql_update, model);
        }

        static int DeleteContent(Content model)
        {
            string sql_delete = @"DELETE FROM [Content] WHERE   (id = @id)";
            return ExecuteNonQuery(sql_delete, model);
        }

        static int DeleteContent(List<Content> model)
        {
            string sql_delete = @"DELETE FROM [Content] WHERE   (id = @id)";
            return ExecuteNonQuery(sql_delete, model);
        }

        static Content GetContent(int id)
        {
            string sql_select = @"select * from [dbo].[content] where id=@id";
            return ExecuteQuery<Content>(sql_select, new { id }).FirstOrDefault();
        }

        static List<Content> GetContentList(int[] ids)
        {
            string sql_select = @"select * from [dbo].[content] where id in @ids";
            return ExecuteQuery<Content>(sql_select, new { ids });
        }


        static ContentWithCommnet GetContentWithComment(int id)
        {
            string sql_mult_query = @"select * from content where id=@id;
                                      select * from comment where content_id=@id";

            using (var conn = new SqlConnection(connStr))
            {
                using (var result = conn.QueryMultiple(sql_mult_query, new { id }))
                {
                    var contentWithComments = result.ReadFirst<ContentWithCommnet>();
                    contentWithComments.comments = result.Read<Comment>().ToList();
                    return contentWithComments;
                }
            }
        }

        static Comment GetComment(int id)
        {
            string sql_mult_query = @"select  m.*,c.* from content c
                                      inner join comment m on m.content_id=c.id
                                      where c.id=@id";

            using (var conn = new SqlConnection(connStr))
            {
                /* splitOn参数：从查询结果所有字段列表的最后一个字段开始进行匹配，一直到找到第一个id字段（大小写忽略无所谓），
                * 最后一个字段到第一个id字段 匹配到 Content对象【query<T1,T2,TResult> 中的T1】
                * id 开始到最前面一个字段匹配到 Comment对象【query<T1,T2,TResult> 中的T2】
                */
                var result = conn.Query<Comment, Content, Comment>(sql_mult_query,
                    (m, c) =>
                   {
                       m.CommentContent = c;
                       return m;
                   }, new { id }, splitOn: "id");

                return result.FirstOrDefault();
            }
        }

        static List<Content> GetCotents()
        {
            string sql_mult_query = @"select  m.*,c.* from content c
                                      left join comment m on m.content_id=c.id";

            var res = new List<Content>();
            using (var conn = new SqlConnection(connStr))
            {
                /* splitOn参数：从查询结果所有字段列表的最后一个字段开始进行匹配，一直到找到第一个id字段（大小写忽略无所谓），
                * 最后一个字段到第一个id字段 匹配到 Content对象【query<T1,T2,TResult> 中的T1】
                * id 开始到最前面一个字段匹配到 Comment对象【query<T1,T2,TResult> 中的T2】
                */
                var result = conn.Query<Comment, Content, Content>(sql_mult_query,
                    (m, c) =>
                    {
                        var currentContent = res.FirstOrDefault(t => t.id == c.id);
                        if (currentContent == null)
                        {
                            c.Comments = new List<Comment>();
                            if (m != null)
                            {
                                c.Comments.Add(m);
                            }

                            if (c != null)
                                res.Add(c);

                            return c;
                        }
                        else
                        {
                            if (m != null && !currentContent.Comments.Any(t => t.id == m.id))
                                currentContent.Comments.Add(m);

                            return currentContent;
                        }

                    }, splitOn: "id");
            }

            return res;

        }

        static int ExecuteNonQuery(string sql, object param = null)
        {
            using (var conn = new SqlConnection(connStr))
            {
                return conn.Execute(sql, param);
            }
        }

        static List<T> ExecuteQuery<T>(string sql, object param = null) where T : class
        {
            using (var conn = new SqlConnection(connStr))
            {
                return conn.Query<T>(sql, param).ToList();
            }
        }

        static List<T> ExecuteMutiQuery<T>(string sql, object param = null) where T : class
        {
            using (var conn = new SqlConnection(connStr))
            {
                using (var result = conn.QueryMultiple(sql, param))
                {
                    return result.Read<T>().ToList();
                }
            }
        }

    }
}
